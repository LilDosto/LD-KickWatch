using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KickStreamHub.Services;

public class ChatMessageEventArgs : EventArgs
{
    public string Username { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string RawMessage { get; init; } = string.Empty;
    public string ColorHex { get; init; } = "#53FC18";
}

/// <summary>
/// Kick.com sohbetinin Pusher WebSocket protokolüne bağlanan istemci.
/// .NET'in ClientWebSocket'i tek bir dedike okuma döngüsünde asenkron
/// olarak kullanıldığından, Python tarafındaki gibi çoklu iş parçacığı
/// çakışması riski yoktur. Bağlantı koptuğunda üstel geri çekilmeyle
/// (exponential backoff) otomatik olarak yeniden bağlanır.
/// </summary>
public sealed partial class PusherChatService : IAsyncDisposable
{
    private static string s_activeAppKey = "34bf7a0ff419a2a775b9";
    private static string s_activeCluster = "us2";

    [GeneratedRegex(@"\[emote:\d+:([^\]]+)\]")]
    private static partial Regex KickEmoteRegex();

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoopTask;
    private long _chatroomId;
    private int _reconnectAttempt;

    public event EventHandler<ChatMessageEventArgs>? MessageReceived;
    public event EventHandler<bool>? ConnectionStateChanged;

    public async Task ConnectAsync(long chatroomId)
    {
        await DisconnectAsync().ConfigureAwait(false);
        _chatroomId = chatroomId;
        _cts = new CancellationTokenSource();
        _receiveLoopTask = RunConnectionLoopAsync(_cts.Token);
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _cts?.Cancel();
            if (_socket is { State: WebSocketState.Open })
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
        catch
        {
            // kapatma sırasında oluşan hatalar yoksayılır
        }
        finally
        {
            _socket?.Dispose();
            _socket = null;
            if (_receiveLoopTask != null)
            {
                try { await _receiveLoopTask.ConfigureAwait(false); } catch { /* yoksay */ }
            }
        }
    }

    private async Task RunConnectionLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectOnceAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Warning($"Sohbet bağlantısı kesildi: {ex.Message}");
            }

            ConnectionStateChanged?.Invoke(this, false);
            if (ct.IsCancellationRequested) break;

            _reconnectAttempt++;
            var delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, _reconnectAttempt)));
            try { await Task.Delay(delay, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task ConnectOnceAsync(CancellationToken ct)
    {
        _socket = new ClientWebSocket();
        var uri = new Uri($"wss://ws-{s_activeCluster}.pusher.com/app/{s_activeAppKey}?protocol=7&client=js&version=8.4.0&flash=false");
        await _socket.ConnectAsync(uri, ct).ConfigureAwait(false);
        _reconnectAttempt = 0;
        ConnectionStateChanged?.Invoke(this, true);

        var buffer = new byte[16384];
        var messageBuilder = new StringBuilder();
        using var pingCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var pingTask = RunPingLoopAsync(pingCts.Token);

        try
        {
            while (_socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                messageBuilder.Clear();
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close) return;
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                await HandleRawMessageAsync(messageBuilder.ToString(), ct).ConfigureAwait(false);
            }
        }
        finally
        {
            pingCts.Cancel();
            try { await pingTask.ConfigureAwait(false); } catch { }
        }
    }

    private async Task RunPingLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
                if (_socket is { State: WebSocketState.Open })
                {
                    await SendAsync("{\"event\":\"pusher:ping\",\"data\":{}}", ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Warning($"Sohbet ping gönderilemedi: {ex.Message}");
            }
        }
    }

    private async Task HandleRawMessageAsync(string raw, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var eventName = root.TryGetProperty("event", out var evEl) ? evEl.GetString() : null;

            switch (eventName)
            {
                case "pusher:connection_established":
                    await SubscribeToChatroomAsync(ct).ConfigureAwait(false);
                    break;

                case "App\\Events\\ChatMessageEvent":
                case "ChatMessageEvent":
                    HandleChatMessagePayload(root);
                    break;

                case "pusher:ping":
                    await SendAsync("{\"event\":\"pusher:pong\",\"data\":{}}", ct).ConfigureAwait(false);
                    break;

                case "pusher:error":
                    LoggingService.Instance.Warning($"Pusher sohbet hatası: {raw}");
                    break;
            }
        }
        catch (JsonException ex)
        {
            LoggingService.Instance.Warning($"Sohbet mesajı ayrıştırılamadı: {ex.Message}");
        }
    }

    private void HandleChatMessagePayload(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var dataEl)) return;
        var dataRaw = dataEl.GetString();
        if (string.IsNullOrEmpty(dataRaw)) return;

        using var innerDoc = JsonDocument.Parse(dataRaw);
        var inner = innerDoc.RootElement;

        var rawContent = inner.TryGetProperty("content", out var c) ? c.GetString() ?? string.Empty : string.Empty;
        var content = KickEmoteRegex().Replace(rawContent, ":$1:");

        var sender = inner.TryGetProperty("sender", out var s) ? s : default;

        var username = "?";
        var color = "#53FC18";
        if (sender.ValueKind == JsonValueKind.Object)
        {
            if (sender.TryGetProperty("username", out var u)) username = u.GetString() ?? "?";
            if (sender.TryGetProperty("identity", out var identity) &&
                identity.TryGetProperty("color", out var colorEl))
            {
                var parsedColor = colorEl.GetString();
                if (!string.IsNullOrWhiteSpace(parsedColor))
                    color = parsedColor;
            }
        }

        MessageReceived?.Invoke(this, new ChatMessageEventArgs
        {
            Username = username,
            Message = content,
            RawMessage = rawContent,
            ColorHex = color,
        });
    }

    private async Task SubscribeToChatroomAsync(CancellationToken ct)
    {
        var channelName = $"chatrooms.{_chatroomId}.v2";
        var payload = JsonSerializer.Serialize(new
        {
            @event = "pusher:subscribe",
            data = new { auth = "", channel = channelName },
        });
        await SendAsync(payload, ct).ConfigureAwait(false);
    }

    private async Task SendAsync(string json, CancellationToken ct)
    {
        if (_socket is not { State: WebSocketState.Open }) return;
        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_socket is not { State: WebSocketState.Open }) return;
            var bytes = Encoding.UTF8.GetBytes(json);
            await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct)
                .ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        _cts?.Dispose();
        _sendLock.Dispose();
    }
}
