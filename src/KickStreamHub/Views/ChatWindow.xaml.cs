using System.Collections.Specialized;
using System.Windows;
using KickStreamHub.ViewModels;

namespace KickStreamHub.Views;

public partial class ChatWindow : Window
{
    private readonly ChatViewModel _vm;

    public ChatWindow(string channelName, long chatroomId, int maxMessages = 500)
    {
        InitializeComponent();
        _vm = new ChatViewModel { MaxMessages = maxMessages };
        DataContext = _vm;

        Loaded += async (_, _) => await _vm.ConnectAsync(channelName, chatroomId);
        Closed += async (_, _) => await _vm.DisposeAsync();

        _vm.FilteredMessages.CollectionChanged += AutoScrollToBottom;
    }

    private void AutoScrollToBottom(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_vm.IsPaused) return;

        if ((e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Reset)
            && ChatListBox.Items.Count > 0)
        {
            ChatListBox.ScrollIntoView(ChatListBox.Items[^1]);
        }
    }
}
