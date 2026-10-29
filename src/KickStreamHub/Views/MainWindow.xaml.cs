using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KickStreamHub.Services;
using KickStreamHub.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace KickStreamHub.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly NotificationService _notifications;
    private bool _isWebViewInitialized;
    private readonly bool[] _isMultiWebViewInitialized = new bool[4];
    private CoreWebView2Environment? _webViewEnv;
    private WindowState _stateBeforeFullScreen = WindowState.Normal;

    public MainWindow()
    {
        InitializeComponent();

        VlcPanel.BackColor = System.Drawing.Color.FromArgb(4, 9, 15);

        _viewModel = new MainViewModel();
        _notifications = new NotificationService();
        _viewModel.Notifications = _notifications;
        DataContext = _viewModel;

        _viewModel.EmbeddedPlayRequested += ViewModel_EmbeddedPlayRequested;
        _viewModel.EmbeddedVlcPlayRequested += ViewModel_EmbeddedVlcPlayRequested;
        _viewModel.EmbeddedStopRequested += ViewModel_EmbeddedStopRequested;
        _viewModel.EmbeddedMuteRequested += ViewModel_EmbeddedMuteRequested;
        _viewModel.EmbeddedReloadRequested += ViewModel_EmbeddedReloadRequested;
        _viewModel.FullScreenChanged += ViewModel_FullScreenChanged;

        _viewModel.MultiSlotPlayRequested += ViewModel_MultiSlotPlayRequested;
        _viewModel.MultiSlotStopRequested += ViewModel_MultiSlotStopRequested;
        _viewModel.MultiSlotMuteRequested += ViewModel_MultiSlotMuteRequested;

        _viewModel.EmbeddedChat.NewMessagesAppended += EmbeddedChat_NewMessagesAppended;

        _notifications.OpenRequested += (_, _) => RestoreAndBringToFront();
        _notifications.ExitRequested += (_, _) => Close();
        _notifications.Show();

        Loaded += (_, _) => _viewModel.Start();
        StateChanged += MainWindow_StateChanged;
        Closing += MainWindow_Closing;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        RestoreWindowBounds();

        var ctrlF = new KeyBinding(new RelayFocusCommand(this), Key.F, ModifierKeys.Control);
        InputBindings.Add(ctrlF);
    }

    private async Task<CoreWebView2Environment> GetOrCreateWebViewEnvAsync()
    {
        if (_webViewEnv != null) return _webViewEnv;
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LDKickWatch", "WebView2");
        Directory.CreateDirectory(userDataFolder);
        _webViewEnv = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        return _webViewEnv;
    }

    private void ViewModel_EmbeddedPlayRequested(object? sender, string channelName)
    {
        _ = Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                if (!_isWebViewInitialized)
                {
                    var env = await GetOrCreateWebViewEnvAsync();
                    await EmbeddedPlayer.EnsureCoreWebView2Async(env);
                    EmbeddedPlayer.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    EmbeddedPlayer.CoreWebView2.Settings.AreDevToolsEnabled = false;

                    EmbeddedPlayer.CoreWebView2.ContainsFullScreenElementChanged += (_, _) =>
                    {
                        _viewModel.SetFullScreenState(EmbeddedPlayer.CoreWebView2.ContainsFullScreenElement);
                    };

                    _isWebViewInitialized = true;
                }

                EmbeddedPlayer.CoreWebView2.IsMuted = false;
                var playerUrl = $"https://player.kick.com/{channelName.ToLowerInvariant()}";
                EmbeddedPlayer.Source = new Uri(playerUrl);
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"Dahili oynatıcı hatası: {ex.Message}";
            }
        });
    }

    private void ViewModel_EmbeddedVlcPlayRequested(object? sender, string streamUrl)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            try
            {
                if (_isWebViewInitialized)
                {
                    EmbeddedPlayer.Source = new Uri("about:blank");
                }

                VlcHost.UpdateLayout();
                var hwnd = VlcPanel.Handle;
                var vlcPath = _viewModel.Streamlink.FindPlayer("vlc", _viewModel.Config.PlayerPath);

                if (vlcPath is null)
                {
                    _viewModel.StatusMessage = "⚠ VLC bulunamadı! Lütfen VLC kurun veya Ayarlardan yolunu seçin.";
                    return;
                }

                var (success, msg) = _viewModel.Streamlink.PlayEmbeddedVlc(
                    _viewModel.ActivePlayerChannel,
                    hwnd,
                    vlcPath,
                    streamUrl,
                    _viewModel.Config.ExtraStreamlinkArgs);

                _viewModel.StatusMessage = msg;
                if (!success)
                {
                    _viewModel.StopEmbeddedPlayer();
                }
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"Pencere içi VLC hatası: {ex.Message}";
            }
        });
    }

    private void ViewModel_EmbeddedStopRequested(object? sender, EventArgs e)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            try
            {
                if (_isWebViewInitialized)
                {
                    EmbeddedPlayer.Source = new Uri("about:blank");
                }
            }
            catch { }
        });
    }

    private void ViewModel_EmbeddedMuteRequested(object? sender, bool isMuted)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            try
            {
                if (_isWebViewInitialized && EmbeddedPlayer.CoreWebView2 != null)
                {
                    EmbeddedPlayer.CoreWebView2.IsMuted = isMuted;
                }
            }
            catch { }
        });
    }

    private void ViewModel_EmbeddedReloadRequested(object? sender, EventArgs e)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            try
            {
                if (_isWebViewInitialized)
                {
                    EmbeddedPlayer.Reload();
                }
            }
            catch { }
        });
    }

    private void ViewModel_FullScreenChanged(object? sender, bool enable)
    {
        Dispatcher.BeginInvoke((Action)(() =>
        {
            if (enable)
            {
                _stateBeforeFullScreen = WindowState;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Normal;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                WindowState = _stateBeforeFullScreen == WindowState.Minimized ? WindowState.Normal : _stateBeforeFullScreen;
            }
        });
    }

    private void VlcPanel_DoubleClick(object? sender, EventArgs e)
    {
        _viewModel.ToggleFullScreenCommand.Execute(null);
    }

    private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _viewModel.IsFullScreen)
        {
            _viewModel.SetFullScreenState(false);
            e.Handled = true;
        }
    }

    // ==================== MULTI-STREAM WEBVIEW2 HANDLERS ====================

    private WebView2? GetMultiWebView(int index) => index switch
    {
        0 => MultiWebView0,
        1 => MultiWebView1,
        2 => MultiWebView2,
        3 => MultiWebView3,
        _ => null,
    };

    private void ViewModel_MultiSlotPlayRequested(object? sender, MultiSlotPlayEventArgs e)
    {
        _ = Dispatcher.InvokeAsync(async () =>
        {
            var wv = GetMultiWebView(e.SlotIndex);
            if (wv is null) return;

            try
            {
                if (!_isMultiWebViewInitialized[e.SlotIndex])
                {
                    var env = await GetOrCreateWebViewEnvAsync();
                    await wv.EnsureCoreWebView2Async(env);
                    wv.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    wv.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    wv.CoreWebView2.ContainsFullScreenElementChanged += (_, _) =>
                    {
                        _viewModel.SetFullScreenState(wv.CoreWebView2.ContainsFullScreenElement);
                    };
                    _isMultiWebViewInitialized[e.SlotIndex] = true;
                }

                wv.CoreWebView2.IsMuted = e.IsMuted;
                wv.Source = new Uri($"https://player.kick.com/{e.ChannelName.ToLowerInvariant()}");
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"Çoklu yayın #{e.SlotIndex + 1} hatası: {ex.Message}";
            }
        });
    }

    private void ViewModel_MultiSlotStopRequested(object? sender, int slotIndex)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            var wv = GetMultiWebView(slotIndex);
            if (wv is null || !_isMultiWebViewInitialized[slotIndex]) return;
            try { wv.Source = new Uri("about:blank"); } catch { }
        });
    }

    private void ViewModel_MultiSlotMuteRequested(object? sender, MultiSlotPlayEventArgs e)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            var wv = GetMultiWebView(e.SlotIndex);
            if (wv is null || !_isMultiWebViewInitialized[e.SlotIndex]) return;
            try
            {
                if (wv.CoreWebView2 != null)
                    wv.CoreWebView2.IsMuted = e.IsMuted;
            }
            catch { }
        });
    }

    // ==================== EMBEDDED CHAT AUTO-SCROLL ====================

    private void EmbeddedChat_NewMessagesAppended(object? sender, EventArgs e)
    {
        if (_viewModel.EmbeddedChat.IsPaused) return;
        try
        {
            if (EmbeddedChatListBox.Items.Count > 0)
            {
                EmbeddedChatListBox.ScrollIntoView(EmbeddedChatListBox.Items[^1]);
            }
        }
        catch { }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ActivePage) && _viewModel.ActivePage == "exit")
        {
            Close();
        }
    }

    private void ChannelList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject dep)
        {
            var current = dep;
            while (current != null && current is not System.Windows.Controls.ListBoxItem)
            {
                if (current is System.Windows.Controls.Primitives.ButtonBase)
                    return;
                current = VisualTreeHelper.GetParent(current);
            }
        }

        if (_viewModel.SelectedChannel is not null)
        {
            _viewModel.PlayChannelCommand.Execute(_viewModel.SelectedChannel.ChannelName);
        }
    }

    private void RestoreWindowBounds()
    {
        var cfg = _viewModel.Config;
        if (cfg.WindowLeft >= 0 && cfg.WindowTop >= 0)
        {
            var isVisibleOnAnyScreen = System.Windows.Forms.Screen.AllScreens.Any(s =>
                s.WorkingArea.IntersectsWith(new System.Drawing.Rectangle(
                    (int)cfg.WindowLeft, (int)cfg.WindowTop,
                    (int)Math.Max(100, cfg.WindowWidth), (int)Math.Max(100, cfg.WindowHeight))));

            if (isVisibleOnAnyScreen)
            {
                Left = cfg.WindowLeft;
                Top = cfg.WindowTop;
            }
        }
        if (cfg.WindowWidth >= MinWidth) Width = cfg.WindowWidth;
        if (cfg.WindowHeight >= MinHeight) Height = cfg.WindowHeight;

        if (cfg.StartMinimized)
        {
            WindowState = WindowState.Minimized;
            if (cfg.MinimizeToTray)
            {
                Hide();
            }
        }
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _viewModel.Config.MinimizeToTray)
            Hide();
    }

    public void RestoreAndBringToFront()
    {
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void QualityButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.ContextMenu != null)
        {
            foreach (var item in fe.ContextMenu.Items.OfType<System.Windows.Controls.MenuItem>())
            {
                var isCurrent = string.Equals(item.Tag as string, _viewModel.SelectedQuality, StringComparison.OrdinalIgnoreCase);
                var headerStr = item.Header?.ToString() ?? string.Empty;
                var clean = headerStr.Replace("✓", "", StringComparison.Ordinal).Trim();
                item.Header = isCurrent ? $"✓  {clean}" : $"    {clean}";
                item.FontWeight = isCurrent ? FontWeights.Bold : FontWeights.Normal;
            }

            fe.ContextMenu.PlacementTarget = fe;
            fe.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            fe.ContextMenu.IsOpen = true;
        }
    }

    private void QualityMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem mi && mi.Tag is string q)
        {
            _viewModel.SetStreamQuality(q);
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        // Pencere kapatıldığında (X veya Alt+F4) uygulama arkaplanda açık kalmaz,
        // tüm oynatıcı/WebView2/sohbet süreçlerini temizleyip tamamen kapanır.
        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        if (!bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
        {
            _viewModel.Config.WindowLeft = bounds.Left;
            _viewModel.Config.WindowTop = bounds.Top;
            _viewModel.Config.WindowWidth = bounds.Width;
            _viewModel.Config.WindowHeight = bounds.Height;
        }

        _viewModel.Stop();
        _notifications.Dispose();

        try { EmbeddedPlayer.Dispose(); } catch { }
        try { MultiWebView0.Dispose(); } catch { }
        try { MultiWebView1.Dispose(); } catch { }
        try { MultiWebView2.Dispose(); } catch { }
        try { MultiWebView3.Dispose(); } catch { }
        try { VlcHost.Dispose(); } catch { }

        // Tüm arkaplan iş parçacıkları ve süreçleriyle birlikte hemen ve tamamen sonlandır
        Environment.Exit(0);
    }

    private void BrowseStreamlink_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Streamlink|streamlink.exe|Tüm Dosyalar|*.*",
            Title = "Streamlink Yolunu Seçin"
        };
        if (dlg.ShowDialog() == true)
        {
            _viewModel.Config.StreamlinkPath = dlg.FileName;
        }
    }

    private void BrowsePlayer_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Oynatıcı Yürütülebilir Dosyası|*.exe|Tüm Dosyalar|*.*",
            Title = "Oynatıcı (VLC/MPV) Yolunu Seçin"
        };
        if (dlg.ShowDialog() == true)
        {
            _viewModel.Config.PlayerPath = dlg.FileName;
        }
    }

    private void BrowseSound_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "WAV Ses Dosyası|*.wav|Tüm Dosyalar|*.*",
            Title = "Bildirim Ses Dosyasını (.wav) Seçin"
        };
        if (dlg.ShowDialog() == true)
        {
            _viewModel.Config.CustomSoundPath = dlg.FileName;
        }
    }

    private void ExportFavorites_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "JSON Yedek Dosyası (*.json)|*.json|CSV Tablo Dosyası (*.csv)|*.csv",
            FileName = $"LDKickWatch_Favoriler_{DateTime.Now:yyyyMMdd}.json",
            Title = "Favori Kanalları Dışa Aktar"
        };
        if (dlg.ShowDialog() == true)
        {
            _viewModel.ExportFavoritesToFile(dlg.FileName);
        }
    }

    private void ImportFavorites_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Desteklenen Dosyalar (*.json;*.csv)|*.json;*.csv|JSON (*.json)|*.json|CSV (*.csv)|*.csv",
            Title = "Favori Kanalları İçe Aktar"
        };
        if (dlg.ShowDialog() == true)
        {
            _viewModel.ImportFavoritesFromFile(dlg.FileName);
        }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveConfig();
        _viewModel.StatusMessage = "✔ Ayarlar kaydedildi ve uygulandı.";
        _viewModel.ActivePage = "home";
    }

    private void CreateDesktopShortcut_Click(object sender, RoutedEventArgs e)
    {
        if (ShortcutService.CreateDesktopShortcut(out string message))
        {
            _viewModel.StatusMessage = $"✔ {message}";
            System.Windows.MessageBox.Show(message, "Masaüstü Kısayolu", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            _viewModel.StatusMessage = $"⚠ {message}";
            System.Windows.MessageBox.Show(message, "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private class RelayFocusCommand : ICommand
    {
        private readonly MainWindow _win;
        public RelayFocusCommand(MainWindow win) => _win = win;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter)
        {
            _win._viewModel.ActivePage = "home";
            if (_win.FindName("SearchBox") is System.Windows.Controls.TextBox tb)
            {
                tb.Focus();
                tb.SelectAll();
            }
        }
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
