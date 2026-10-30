using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KickStreamHub.Helpers;
using KickStreamHub.Services;
using Microsoft.Web.WebView2.Core;

namespace KickStreamHub.Views;

public partial class PlayerWindow : Window
{
    private readonly string _channelName;
    private readonly KickApiService _api = new();
    private readonly StreamlinkService _streamlink = new();
    private Process? _vlcProcess;
    private bool _isVlcActive;
    private bool _isFullScreen;
    private WindowState _previousState = WindowState.Normal;
    private string _currentQuality = "best";
    private string _cachedPlaybackUrl = string.Empty;

    private static readonly SolidColorBrush WebActiveBg = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A3822"));
    private static readonly SolidColorBrush WebActiveFg = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#53FC18"));
    private static readonly SolidColorBrush VlcActiveBg = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2A2410"));
    private static readonly SolidColorBrush VlcActiveFg = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F59E0B"));
    private static readonly SolidColorBrush PassiveBg = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#141D2B"));
    private static readonly SolidColorBrush PassiveFg = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F8FAFC"));
    private static readonly SolidColorBrush PassiveBorder = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E2D42"));

    public PlayerWindow(string channelName)
    {
        InitializeComponent();
        _channelName = channelName;
        Title = $"{channelName.ToUpperInvariant()} — Canlı Yayın (LD KickWatch)";
        ChannelTitleText.Text = channelName.ToUpperInvariant();
        UpdateEngineButtons();
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

    private void UpdateEngineButtons()
    {
        if (_isVlcActive)
        {
            VlcEngineBtn.Background = VlcActiveBg;
            VlcEngineBtn.Foreground = VlcActiveFg;
            VlcEngineBtn.BorderBrush = VlcActiveFg;
            VlcEngineBtn.FontWeight = FontWeights.Bold;

            WebEngineBtn.Background = PassiveBg;
            WebEngineBtn.Foreground = PassiveFg;
            WebEngineBtn.BorderBrush = PassiveBorder;
            WebEngineBtn.FontWeight = FontWeights.Normal;

            VlcHost.Visibility = Visibility.Visible;
            WebView.Visibility = Visibility.Collapsed;
            MuteBtn.Visibility = Visibility.Collapsed;
        }
        else
        {
            WebEngineBtn.Background = WebActiveBg;
            WebEngineBtn.Foreground = WebActiveFg;
            WebEngineBtn.BorderBrush = WebActiveFg;
            WebEngineBtn.FontWeight = FontWeights.Bold;

            VlcEngineBtn.Background = PassiveBg;
            VlcEngineBtn.Foreground = PassiveFg;
            VlcEngineBtn.BorderBrush = PassiveBorder;
            VlcEngineBtn.FontWeight = FontWeights.Normal;

            WebView.Visibility = Visibility.Visible;
            VlcHost.Visibility = Visibility.Collapsed;
            MuteBtn.Visibility = Visibility.Visible;
        }
    }

    private void WebEngineBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!_isVlcActive) return;
        _isVlcActive = false;
        StopVlc();
        UpdateEngineButtons();

        try
        {
            var playerUrl = $"https://player.kick.com/{_channelName.ToLowerInvariant()}";
            WebView.Source = new Uri(playerUrl);
        }
        catch { }
    }

    private async void VlcEngineBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_isVlcActive) return;
        await StartVlcAsync();
    }

    private async Task StartVlcAsync()
    {
        var vlcPath = _streamlink.FindPlayer("vlc", null);
        if (vlcPath is null)
        {
            System.Windows.MessageBox.Show(
                "VLC Media Player bulunamadı! Lütfen bilgisayarınıza VLC kurun.",
                "VLC Bulunamadı",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        StatusSubText.Text = "⏳ VLC yayını hazırlanıyor...";
        _isVlcActive = true;
        UpdateEngineButtons();

        try
        {
            if (WebView.CoreWebView2 != null)
            {
                WebView.Source = new Uri("about:blank");
            }
        }
        catch { }

        try
        {
            if (string.IsNullOrEmpty(_cachedPlaybackUrl))
            {
                var channelData = await _api.GetChannelAsync(_channelName).ConfigureAwait(true);
                if (channelData?.Livestream is null || string.IsNullOrWhiteSpace(channelData.PlaybackUrl))
                {
                    StatusSubText.Text = "🔴 Kanal şu anda canlı yayında değil.";
                    System.Windows.MessageBox.Show(
                        $"{_channelName.ToUpperInvariant()} kanalı şu anda canlı yayında değil veya akış linki alınamadı.",
                        "Yayın Kapalı",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    WebEngineBtn_Click(this, new RoutedEventArgs());
                    return;
                }
                _cachedPlaybackUrl = channelData.PlaybackUrl;
            }

            var streamUrl = await _api.ResolveQualityUrlAsync(_cachedPlaybackUrl, _currentQuality).ConfigureAwait(true);

            VlcHost.UpdateLayout();
            var hwnd = VlcPanel.Handle;

            StopVlc();

            var args = $"\"{streamUrl}\" -I dummy --dummy-quiet --drawable-hwnd={hwnd.ToInt64()} --no-video-title-show --network-caching=800 --clock-jitter=0 --no-sub-autodetect-file";
            var psi = new ProcessStartInfo
            {
                FileName = vlcPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            var proc = Process.Start(psi);
            if (proc is null)
            {
                StatusSubText.Text = "⚠ VLC işlemi başlatılamadı.";
                return;
            }

            ChildProcessTracker.AddProcess(proc);
            proc.EnableRaisingEvents = true;
            proc.Exited += (_, _) =>
            {
                _ = Dispatcher.BeginInvoke(() =>
                {
                    if (ReferenceEquals(_vlcProcess, proc))
                    {
                        _vlcProcess = null;
                        if (_isVlcActive)
                        {
                            StatusSubText.Text = "⏹ VLC yayını sonlandı.";
                        }
                    }
                });
            };

            _vlcProcess = proc;
            StatusSubText.Text = $"🎬 VLC aktif ({_currentQuality}) • F11 Tam Ekran";
        }
        catch (Exception ex)
        {
            StatusSubText.Text = $"VLC Hatası: {ex.Message}";
            LoggingService.Instance.Error($"PlayerWindow VLC başlatılamadı ({_channelName})", ex);
        }
    }

    private void StopVlc()
    {
        if (_vlcProcess != null)
        {
            try
            {
                if (!_vlcProcess.HasExited)
                    _vlcProcess.Kill(true);
            }
            catch { }
            _vlcProcess = null;
        }
    }

    private void QualityButton_Click(object sender, RoutedEventArgs e)
    {
        if (QualityButton.ContextMenu != null)
        {
            foreach (var item in QualityButton.ContextMenu.Items)
            {
                if (item is MenuItem mi && mi.Tag is string tag)
                {
                    mi.IsChecked = string.Equals(tag, _currentQuality, StringComparison.OrdinalIgnoreCase);
                }
            }
            QualityButton.ContextMenu.PlacementTarget = QualityButton;
            QualityButton.ContextMenu.IsOpen = true;
        }
    }

    private async void QualityMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is string newQuality)
        {
            _currentQuality = newQuality;
            QualityDisplayText.Text = mi.Header?.ToString() ?? newQuality;

            if (_isVlcActive)
            {
                await StartVlcAsync();
            }
        }
    }

    private void VlcPanel_DoubleClick(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() => SetFullScreen(!_isFullScreen));
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
            WindowState = WindowState.Normal;
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

    private async void ReloadBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_isVlcActive)
        {
            _cachedPlaybackUrl = string.Empty;
            await StartVlcAsync();
        }
        else
        {
            try { WebView.Reload(); } catch { }
        }
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
        StopVlc();
        try { WebView.Dispose(); } catch { }
        base.OnClosed(e);
    }
}
