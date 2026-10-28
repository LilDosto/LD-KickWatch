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

        _vm.NewMessagesAppended += (_, _) => AutoScrollToBottom();
    }

    private void AutoScrollToBottom()
    {
        if (_vm.IsPaused) return;

        try
        {
            if (ChatListBox.Items.Count > 0)
            {
                ChatListBox.ScrollIntoView(ChatListBox.Items[^1]);
            }
        }
        catch { }
    }
}
