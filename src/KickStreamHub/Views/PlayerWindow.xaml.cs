using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace KickStreamHub.Views;

public partial class PlayerWindow : Window
{
    private readonly string _channelName;
    private bool _isFullScreen;
    private WindowState _previousState = WindowState.Normal;

    public PlayerWindow(string channelName)
    {
        InitializeComponent();
        _channelName = channelName;
        Title = $"{channelName.ToUpperInvariant()} — Canlı Yayın (LD KickWatch)";
        ChannelTitleText.Text = $"{channelName.ToUpperInvariant()} — LD KickWatch Oynatıcı";
        Loaded += PlayerWindow_Loaded;
    }

    private async void PlayerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LDKickWatch", "WebView2");
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await WebView.EnsureCoreWebView2Async(env);

            WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

            // HTML5 Video Tam Ekran (requestFullscreen) olayını yakala
            WebView.CoreWebView2.ContainsFullScreenElementChanged += (_, _) =>
            {
                SetFullScreen(WebView.CoreWebView2.ContainsFullScreenElement);
            };

            var playerUrl = $"https://player.kick.com/{_channelName.ToLowerInvariant()}";
            WebView.Source = new Uri(playerUrl);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Oynatıcı başlatılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetFullScreen(bool enable)
    {
        if (_isFullScreen == enable) return;
        _isFullScreen = enable;

        if (enable)
        {
            _previousState = WindowState;
            TopBar.Visibility = Visibility.Collapsed;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Normal; // WPF'de tam ekran geçişi için önce Normal sonra Maximized yapılır
            WindowState = WindowState.Maximized;
        }
        else
        {
            TopBar.Visibility = Visibility.Visible;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = _previousState == WindowState.Minimized ? WindowState.Normal : _previousState;
        }
    }

    private void FullScreenBtn_Click(object sender, RoutedEventArgs e) => SetFullScreen(!_isFullScreen);

    private void ReloadBtn_Click(object sender, RoutedEventArgs e)
    {
        try { WebView.Reload(); } catch { }
    }

    private void MuteBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (WebView.CoreWebView2 != null)
            {
                WebView.CoreWebView2.IsMuted = !WebView.CoreWebView2.IsMuted;
                MuteBtn.Content = WebView.CoreWebView2.IsMuted ? "🔇 Sesi Aç" : "🔊 Sesi Kapat";
            }
        }
        catch { }
    }

    private void TopMostCheck_Changed(object sender, RoutedEventArgs e)
    {
        Topmost = TopMostCheck.IsChecked == true;
    }

    private void PlayerWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            SetFullScreen(!_isFullScreen);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _isFullScreen)
        {
            SetFullScreen(false);
            e.Handled = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        try { WebView.Dispose(); } catch { }
        base.OnClosed(e);
    }
}
