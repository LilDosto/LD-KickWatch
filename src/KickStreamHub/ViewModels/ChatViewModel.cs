using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KickStreamHub.Services;

namespace KickStreamHub.ViewModels;

public class ChatMessageItem
{
    public string Username { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string RawMessage { get; init; } = string.Empty;
    public string ColorHex { get; init; } = "#53FC18";
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string TimeFormatted => Timestamp.ToString("HH:mm");
}

public partial class ChatViewModel : ObservableObject, IAsyncDisposable
{
    private readonly PusherChatService _chatService = new();
    private readonly Dispatcher _dispatcher;
    public int MaxMessages { get; set; } = 500;

    [ObservableProperty] private string _channelName = string.Empty;
    [ObservableProperty] private long _chatroomId;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private double _fontSize = 13;
    [ObservableProperty] private double _backgroundOpacity = 1.0;
    [ObservableProperty] private bool _isTopMost;
    [ObservableProperty] private int _messageCount;

    public ObservableCollection<ChatMessageItem> Messages { get; } = new();
    public ObservableCollection<ChatMessageItem> FilteredMessages { get; } = new();

    public ChatViewModel()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _chatService.MessageReceived += OnMessageReceived;
        _chatService.ConnectionStateChanged += (_, connected) =>
            _dispatcher.BeginInvoke(() => IsConnected = connected);
    }

    public async Task ConnectAsync(string channelName, long chatroomId)
    {
        if (IsConnected && ChatroomId == chatroomId && string.Equals(ChannelName, channelName, StringComparison.OrdinalIgnoreCase))
            return;

        ChannelName = channelName;
        ChatroomId = chatroomId;
        Messages.Clear();
        FilteredMessages.Clear();
        MessageCount = 0;
        if (chatroomId > 0)
            await _chatService.ConnectAsync(chatroomId);
    }

    public async Task DisconnectAsync()
    {
        await _chatService.DisconnectAsync();
        IsConnected = false;
    }

    private void OnMessageReceived(object? sender, ChatMessageEventArgs e)
    {
        var item = new ChatMessageItem
        {
            Username = e.Username,
            Message = e.Message,
            RawMessage = string.IsNullOrEmpty(e.RawMessage) ? e.Message : e.RawMessage,
            ColorHex = e.ColorHex,
        };

        _dispatcher.BeginInvoke(() =>
        {
            Messages.Add(item);
            while (Messages.Count > MaxMessages)
                Messages.RemoveAt(0);

            MessageCount = Messages.Count;

            if (!IsPaused && MatchesFilter(item))
            {
                FilteredMessages.Add(item);
                while (FilteredMessages.Count > MaxMessages)
                    FilteredMessages.RemoveAt(0);
            }
        });
    }

    private bool MatchesFilter(ChatMessageItem item)
    {
        if (string.IsNullOrWhiteSpace(FilterText)) return true;
        var q = FilterText.Trim();
        return item.Username.Contains(q, StringComparison.OrdinalIgnoreCase) ||
               item.Message.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnFilterTextChanged(string value) => RebuildFilteredMessages();

    partial void OnIsPausedChanged(bool value)
    {
        if (!value)
            RebuildFilteredMessages();
    }

    private void RebuildFilteredMessages()
    {
        FilteredMessages.Clear();
        foreach (var msg in Messages)
        {
            if (MatchesFilter(msg))
                FilteredMessages.Add(msg);
        }
    }

    [RelayCommand]
    private void TogglePause() => IsPaused = !IsPaused;

    [RelayCommand]
    private void ClearMessages()
    {
        Messages.Clear();
        FilteredMessages.Clear();
        MessageCount = 0;
    }

    public async ValueTask DisposeAsync()
    {
        await _chatService.DisposeAsync();
    }
}
