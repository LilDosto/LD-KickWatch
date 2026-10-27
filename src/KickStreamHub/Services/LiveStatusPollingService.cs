using System.Collections.Concurrent;
using KickStreamHub.Models;

namespace KickStreamHub.Services;

public class LiveStatusResultEventArgs : EventArgs
{
    public IReadOnlyDictionary<string, ChannelStatus> Results { get; }
    public LiveStatusResultEventArgs(IReadOnlyDictionary<string, ChannelStatus> results) => Results = results;
}

/// <summary>
/// Favori kanalların canlı yayın durumunu belirli aralıklarla, sınırlı
/// eşzamanlılıkla (SemaphoreSlim) sorgular. .NET HttpClient thread-safe
/// olduğu için burada onlarca kanal aynı anda güvenle sorgulanabilir.
/// </summary>
public sealed class LiveStatusPollingService : IDisposable
{
    private readonly KickApiService _api;
    private readonly SemaphoreSlim _concurrencyLimiter = new(6);
    private System.Threading.Timer? _timer;
    private int _isPolling;
    private IReadOnlyList<string> _channels = Array.Empty<string>();

    public event EventHandler<LiveStatusResultEventArgs>? ResultsReady;
    public event EventHandler<string>? StatusMessage;

    public LiveStatusPollingService(KickApiService api)
    {
        _api = api;
    }

    public void UpdateChannelList(IReadOnlyList<string> channels) => _channels = channels;

    public void Start(int intervalSeconds)
    {
        Stop();
        _timer = new System.Threading.Timer(async _ => await PollOnceSafeAsync().ConfigureAwait(false),
            null, TimeSpan.Zero, TimeSpan.FromSeconds(intervalSeconds));
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public Task RefreshNowAsync() => PollOnceSafeAsync(isManual: true);

    private async Task PollOnceSafeAsync(bool isManual = false)
    {
        if (Interlocked.CompareExchange(ref _isPolling, 1, 0) != 0) return;
        try
        {
            await PollOnceAsync(isManual).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LoggingService.Instance.Error("Canlı yayın kontrolü sırasında hata", ex);
        }
        finally
        {
            Interlocked.Exchange(ref _isPolling, 0);
        }
    }

    private async Task PollOnceAsync(bool isManual = false)
    {
        var channels = _channels;
        if (channels.Count == 0) return;

        if (isManual)
        {
            StatusMessage?.Invoke(this, $"{channels.Count} kanal sorgulanıyor...");
        }
        var results = new ConcurrentDictionary<string, ChannelStatus>();

        var tasks = channels.Select(async channel =>
        {
            await _concurrencyLimiter.WaitAsync().ConfigureAwait(false);
            try
            {
                var status = await FetchOneAsync(channel).ConfigureAwait(false);
                results[channel] = status;
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        ResultsReady?.Invoke(this, new LiveStatusResultEventArgs(results));
        StatusMessage?.Invoke(this, $"✔ {channels.Count} kanal güncellendi.");
    }

    private async Task<ChannelStatus> FetchOneAsync(string channel)
    {
        var data = await _api.GetChannelAsync(channel).ConfigureAwait(false);
        if (data is null)
        {
            return new ChannelStatus { ChannelName = channel, FetchSucceeded = false };
        }

        var live = data.Livestream;
        return new ChannelStatus
        {
            ChannelName = channel,
            IsLive = live is not null,
            Title = live?.SessionTitle ?? string.Empty,
            Category = live?.Categories?.FirstOrDefault()?.Name ?? string.Empty,
            Viewers = live?.ViewerCount ?? 0,
            Verified = data.Verified,
            ProfilePicUrl = data.User?.ProfilePic ?? string.Empty,
            PlaybackUrl = data.PlaybackUrl ?? string.Empty,
            ChatroomId = data.Chatroom?.Id,
            FetchSucceeded = true,
        };
    }

    public void Dispose()
    {
        Stop();
        _concurrencyLimiter.Dispose();
    }
}
