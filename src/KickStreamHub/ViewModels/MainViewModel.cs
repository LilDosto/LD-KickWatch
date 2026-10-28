using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KickStreamHub.Models;
using KickStreamHub.Services;
using KickStreamHub.Views;

namespace KickStreamHub.ViewModels;

public class MultiSlotPlayEventArgs : EventArgs
{
    public int SlotIndex { get; init; }
    public string ChannelName { get; init; } = string.Empty;
    public bool IsMuted { get; init; }
}

public partial class MainViewModel : ObservableObject
{
    private const string AllGroupsLabel = "Tüm Gruplar";

    private readonly ConfigService _configService;
    private readonly KickApiService _api;
    private readonly AvatarCacheService _avatarCache;
    private readonly LiveStatusPollingService _poller;
    public readonly StreamlinkService Streamlink;
    public NotificationService? Notifications { get; set; }

    private readonly Dictionary<string, ChannelStatus> _statusCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _notifiedLive = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _notifiedKeywords = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ChatWindow> _openChatWindows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PlayerWindow> _openPlayerWindows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dispatcher _dispatcher;

    public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
    public string LogDirectoryPath => LoggingService.Instance.LogDirectory;

    public AppConfig Config { get; }

    public ObservableCollection<ChannelCardViewModel> Channels { get; } = new();
    public ObservableCollection<string> GroupFilterOptions { get; } = new() { AllGroupsLabel };
    public ObservableCollection<MultiStreamSlotViewModel> MultiSlots { get; } = new();

    // Embedded (Split-View) Chat ViewModel
    public ChatViewModel EmbeddedChat { get; } = new();

    [ObservableProperty] private string _statusMessage = "Hazır.";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _onlyLiveFilter;
    [ObservableProperty] private string _selectedGroupFilter = AllGroupsLabel;
    [ObservableProperty] private int _visibleChannelCount;
    [ObservableProperty] private ChannelCardViewModel? _selectedChannel;
    [ObservableProperty] private string _addChannelText = string.Empty;
    [ObservableProperty] private string _addChannelGroupText = string.Empty;
    [ObservableProperty] private string _bulkImportText = string.Empty;
    [ObservableProperty] private string _activePage = "home";
    [ObservableProperty] private int _totalChannels;
    [ObservableProperty] private int _liveCount;
    [ObservableProperty] private long _totalViewers;
    [ObservableProperty] private bool _isRefreshing;

    // Detail panel & Embedded Player bindings
    [ObservableProperty] private bool _hasSelectedChannel;
    [ObservableProperty] private string _detailName = "KANAL SEÇİN";
    [ObservableProperty] private string _detailTitle = "Listeden bir yayıncı seçin.";
    [ObservableProperty] private string _detailCategory = string.Empty;
    [ObservableProperty] private string _detailViewers = string.Empty;
    [ObservableProperty] private string _detailGroupInput = string.Empty;
    [ObservableProperty] private bool _detailIsLive;
    [ObservableProperty] private bool _detailVerified;
    [ObservableProperty] private string? _detailAvatarPath;
    [ObservableProperty] private bool _detailIsPlaying;

    [ObservableProperty] private bool _isEmbeddedPlaying;
    [ObservableProperty] private bool _isEmbeddedVlcActive;
    [ObservableProperty] private bool _isEmbeddedWebActive;
    [ObservableProperty] private bool _isEmbeddedMuted;
    [ObservableProperty] private bool _isTheaterMode;
    [ObservableProperty] private bool _isFullScreen;
    [ObservableProperty] private bool _isEmbeddedChatOpen;
    [ObservableProperty] private string _activePlayerChannel = string.Empty;
    [ObservableProperty] private string _selectedQuality = "best";

    // Multi-Stream (Squad View) bindings
    [ObservableProperty] private bool _isQuadLayout = true;

    public event EventHandler<string>? EmbeddedPlayRequested;
    public event EventHandler<string>? EmbeddedVlcPlayRequested;
    public event EventHandler? EmbeddedStopRequested;
    public event EventHandler<bool>? EmbeddedMuteRequested;
    public event EventHandler? EmbeddedReloadRequested;
    public event EventHandler<bool>? FullScreenChanged;

    public event EventHandler<MultiSlotPlayEventArgs>? MultiSlotPlayRequested;
    public event EventHandler<int>? MultiSlotStopRequested;
    public event EventHandler<MultiSlotPlayEventArgs>? MultiSlotMuteRequested;

    public MainViewModel()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _configService = new ConfigService();
        Config = _configService.Load();
        Config.RunOnStartup = StartupService.IsRunOnStartup();
        _isEmbeddedChatOpen = Config.ShowEmbeddedChat;
        _selectedQuality = string.IsNullOrWhiteSpace(Config.StreamQuality) ? "best" : Config.StreamQuality;

        _api = new KickApiService();
        _avatarCache = new AvatarCacheService(_configService.AvatarCacheDirectory, _api);
        Streamlink = new StreamlinkService();
        Streamlink.ProcessExited += (_, _) => _dispatcher.BeginInvoke(() =>
        {
            if (IsEmbeddedVlcActive && !Streamlink.IsPlaying(ActivePlayerChannel))
            {
                IsEmbeddedPlaying = false;
                IsEmbeddedVlcActive = false;
                IsEmbeddedWebActive = false;
                ActivePlayerChannel = string.Empty;
            }
            UpdateDetailPlayState();
        });

        _poller = new LiveStatusPollingService(_api);
        _poller.ResultsReady += OnResultsReady;
        _poller.StatusMessage += (_, msg) => _dispatcher.BeginInvoke(() => StatusMessage = msg);

        for (int i = 0; i < 4; i++)
        {
            MultiSlots.Add(new MultiStreamSlotViewModel { SlotIndex = i, IsMuted = i != 0 });
        }

        foreach (var fav in Config.Favorites)
        {
            Config.ChannelGroups.TryGetValue(fav, out var grp);
            var card = new ChannelCardViewModel(fav, grp ?? string.Empty);
            if (_avatarCache.TryGetFreshCachedPath(fav, out var cachedAvatar))
                card.AvatarPath = cachedAvatar;
            Channels.Add(card);
        }

        RebuildGroupFilterOptions();
        TotalChannels = Channels.Count;
        VisibleChannelCount = Channels.Count;
        _poller.UpdateChannelList(Config.Favorites.ToList());

        if (Channels.Count > 0)
            SelectedChannel = Channels[0];
    }

    public void Start()
    {
        _poller.Start(Math.Max(20, Config.RefreshIntervalSeconds));
        _avatarCache.CleanupStale(Config.Favorites);
        LoggingService.Instance.Info("LD KickWatch başlatıldı.");
    }

    public void Stop()
    {
        _poller.Stop();
        StopEmbeddedPlayer();
        Streamlink.StopAll();
        _ = EmbeddedChat.DisposeAsync();

        foreach (var win in _openChatWindows.Values.ToList())
        {
            try { win.Close(); } catch { }
        }
        foreach (var win in _openPlayerWindows.Values.ToList())
        {
            try { win.Close(); } catch { }
        }

        Config.ShowEmbeddedChat = IsEmbeddedChatOpen;
        _configService.Save(Config);
    }

    public void SaveConfig()
    {
        Config.ShowEmbeddedChat = IsEmbeddedChatOpen;
        Config.StreamQuality = SelectedQuality;
        _configService.Save(Config);
        StartupService.SetRunOnStartup(Config.RunOnStartup);
        _poller.Start(Math.Max(20, Config.RefreshIntervalSeconds));
        UpdateDetailPlayState();
    }

    partial void OnSelectedQualityChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        Config.StreamQuality = value;
        _configService.Save(Config);

        if (IsEmbeddedPlaying && IsEmbeddedVlcActive && !string.IsNullOrEmpty(ActivePlayerChannel))
        {
            _ = PlayEmbeddedVlcPlayerAsync(ActivePlayerChannel);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsRefreshing) return;
        IsRefreshing = true;
        try
        {
            await _poller.RefreshNowAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void AddChannel()
    {
        var raw = AddChannelText.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return;

        var urlMatch = Regex.Match(raw, @"kick\.com/([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
        var name = (urlMatch.Success ? urlMatch.Groups[1].Value : raw.TrimStart('@').Trim('/')).ToLowerInvariant();

        if (!Regex.IsMatch(name, "^[a-z0-9_-]{2,30}$"))
        {
            StatusMessage = "⚠ Geçersiz kanal adı. Yalnızca harf, rakam, tire ve alt çizgi kullanılabilir.";
            return;
        }
        if (Config.Favorites.Any(f => f.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = $"ℹ '{name}' kanalı zaten takip listenizde.";
            return;
        }

        var grp = AddChannelGroupText.Trim();
        Config.Favorites.Add(name);
        if (!string.IsNullOrWhiteSpace(grp))
            Config.ChannelGroups[name] = grp;

        var newCard = new ChannelCardViewModel(name, grp);
        if (_avatarCache.TryGetFreshCachedPath(name, out var cachedAvatar))
            newCard.AvatarPath = cachedAvatar;

        Channels.Add(newCard);
        SelectedChannel = newCard;
        AddChannelText = string.Empty;

        RebuildGroupFilterOptions();
        ApplyFilter();
        UpdateStats();
        _poller.UpdateChannelList(Config.Favorites.ToList());
        _configService.Save(Config);
        StatusMessage = $"✔ '{name}' kanalı eklendi, durum sorgulanıyor...";
        _ = _poller.RefreshNowAsync();
    }

    [RelayCommand]
    private void SaveChannelGroup()
    {
        if (SelectedChannel is null) return;
        var grp = DetailGroupInput.Trim();
        SelectedChannel.GroupName = grp;

        if (string.IsNullOrWhiteSpace(grp))
            Config.ChannelGroups.Remove(SelectedChannel.ChannelName);
        else
            Config.ChannelGroups[SelectedChannel.ChannelName] = grp;

        RebuildGroupFilterOptions();
        ApplyFilter();
        _configService.Save(Config);
        StatusMessage = string.IsNullOrWhiteSpace(grp)
            ? $"🏷 '{SelectedChannel.ChannelName}' grup etiketi kaldırıldı."
            : $"🏷 '{SelectedChannel.ChannelName}' kanalı '{grp}' grubuna atandı.";
    }

    private void RebuildGroupFilterOptions()
    {
        var current = SelectedGroupFilter;
        var groups = Channels
            .Select(c => c.GroupName)
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g)
            .ToList();

        GroupFilterOptions.Clear();
        GroupFilterOptions.Add(AllGroupsLabel);
        foreach (var g in groups)
            GroupFilterOptions.Add(g);

        SelectedGroupFilter = GroupFilterOptions.Contains(current) ? current : AllGroupsLabel;
    }

    [RelayCommand]
    private void RemoveChannel(string? channelName)
    {
        channelName ??= SelectedChannel?.ChannelName;
        if (string.IsNullOrEmpty(channelName)) return;

        if (IsEmbeddedPlaying && string.Equals(ActivePlayerChannel, channelName, StringComparison.OrdinalIgnoreCase))
            StopEmbeddedPlayer();

        Streamlink.Stop(channelName);

        var card = Channels.FirstOrDefault(c => c.ChannelName.Equals(channelName, StringComparison.OrdinalIgnoreCase));
        if (card != null) Channels.Remove(card);

        var fav = Config.Favorites.FirstOrDefault(f => f.Equals(channelName, StringComparison.OrdinalIgnoreCase));
        if (fav != null) Config.Favorites.Remove(fav);

        Config.ChannelGroups.Remove(channelName);
        _statusCache.Remove(channelName);
        _notifiedLive.Remove(channelName);

        RebuildGroupFilterOptions();
        ApplyFilter();
        UpdateStats();
        _poller.UpdateChannelList(Config.Favorites.ToList());
        _configService.Save(Config);

        if (SelectedChannel is null || SelectedChannel.ChannelName.Equals(channelName, StringComparison.OrdinalIgnoreCase))
        {
            SelectedChannel = Channels.FirstOrDefault(c => c.IsVisible) ?? Channels.FirstOrDefault();
            if (SelectedChannel is null)
                ResetDetailPanel();
        }

        StatusMessage = $"🗑 '{channelName}' listeden kaldırıldı.";
    }

    [RelayCommand]
    private void PlayChannel(string? channelName)
    {
        channelName ??= SelectedChannel?.ChannelName;
        if (string.IsNullOrEmpty(channelName)) return;

        var targetCard = Channels.FirstOrDefault(c => c.ChannelName.Equals(channelName, StringComparison.OrdinalIgnoreCase));
        if (targetCard != null)
            SelectedChannel = targetCard;

        // 1. Pencere İçi VLC Oynatıcı seçiliyse
        if (string.Equals(Config.PlayerChoice, "embedded_vlc", StringComparison.OrdinalIgnoreCase))
        {
            if (IsEmbeddedPlaying && IsEmbeddedVlcActive && string.Equals(ActivePlayerChannel, channelName, StringComparison.OrdinalIgnoreCase))
            {
                StopEmbeddedPlayer();
                StatusMessage = "⏹ Pencere içi VLC yayını durduruldu.";
            }
            else
            {
                _ = PlayEmbeddedVlcPlayerAsync(channelName);
            }
            UpdateDetailPlayState();
            return;
        }

        // 2. Harici VLC veya MPV Oynatıcı seçiliyse
        if (string.Equals(Config.PlayerChoice, "vlc", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Config.PlayerChoice, "mpv", StringComparison.OrdinalIgnoreCase))
        {
            var streamlinkPath = Streamlink.FindStreamlink(Config.StreamlinkPath);
            var playerPath = Streamlink.FindPlayer(Config.PlayerChoice, Config.PlayerPath);
            if (streamlinkPath is null)
            {
                StatusMessage = "⚠ Streamlink bulunamadı! Harici oynatıcı için Streamlink kurun veya 'Pencere İçi Oynatıcı' modunu kullanın.";
                return;
            }
            if (playerPath is null)
            {
                StatusMessage = $"⚠ {Config.PlayerChoice.ToUpperInvariant()} bulunamadı! Ayarlardan yolu belirtin.";
                return;
            }

            var (success, message) = Streamlink.Play(channelName, streamlinkPath, playerPath, Config.StreamQuality, Config.ExtraStreamlinkArgs);
            StatusMessage = message;

            if (success && Config.AutoOpenChat && Streamlink.IsPlaying(channelName))
                OpenChat(channelName);

            UpdateDetailPlayState();
            return;
        }

        // 3. Varsayılan: Pencere İçi Web Oynatıcı (WebView2)
        if (IsEmbeddedPlaying && IsEmbeddedWebActive && string.Equals(ActivePlayerChannel, channelName, StringComparison.OrdinalIgnoreCase))
        {
            StopEmbeddedPlayer();
            StatusMessage = "⏹ Dahili yayın durduruldu.";
        }
        else
        {
            PlayEmbeddedWebPlayer(channelName);
            StatusMessage = $"▶ {channelName.ToUpperInvariant()} yayını Web oynatıcıda açıldı.";
        }
        UpdateDetailPlayState();
    }

    public void PlayEmbeddedWebPlayer(string channelName)
    {
        Streamlink.StopEmbeddedVlc();
        ActivePlayerChannel = channelName;
        IsEmbeddedVlcActive = false;
        IsEmbeddedWebActive = true;
        IsEmbeddedPlaying = true;
        IsEmbeddedMuted = false;
        EmbeddedPlayRequested?.Invoke(this, channelName);

        if (IsEmbeddedChatOpen)
            SyncEmbeddedChat(channelName);
        else if (Config.AutoOpenChat)
            OpenChat(channelName);
    }

    public async Task PlayEmbeddedVlcPlayerAsync(string channelName)
    {
        var vlcPath = Streamlink.FindPlayer("vlc", Config.PlayerPath);
        if (vlcPath is null)
        {
            StatusMessage = "⚠ VLC Media Player bulunamadı! Lütfen VLC kurun veya Ayarlardan yolunu seçin.";
            return;
        }

        StatusMessage = $"⏳ {channelName.ToUpperInvariant()} VLC yayını hazırlanıyor...";

        // Kanalın canlı durumunu ve playback_url'sini en güncel şekilde al
        var status = GetStatus(channelName);
        string? playbackUrl = status?.PlaybackUrl;

        if (string.IsNullOrWhiteSpace(playbackUrl) || status is null || !status.IsLive)
        {
            var freshData = await _api.GetChannelAsync(channelName).ConfigureAwait(false);
            if (freshData?.Livestream is null)
            {
                _dispatcher.Invoke(() =>
                {
                    StatusMessage = $"🔴 {channelName.ToUpperInvariant()} şu anda canlı yayında değil.";
                });
                return;
            }
            playbackUrl = freshData.PlaybackUrl;
            if (freshData.Livestream != null && status != null)
            {
                status.PlaybackUrl = freshData.PlaybackUrl ?? string.Empty;
                status.IsLive = true;
            }
        }

        if (string.IsNullOrWhiteSpace(playbackUrl))
        {
            _dispatcher.Invoke(() =>
            {
                StatusMessage = $"🔴 {channelName.ToUpperInvariant()} canlı akış linki alınamadı.";
            });
            return;
        }

        // İstenen kaliteye (best, 720p60, 480p vb.) ait doğrudan akış URL'sini çöz
        var streamUrl = await _api.ResolveQualityUrlAsync(playbackUrl, Config.StreamQuality).ConfigureAwait(false);

        _dispatcher.Invoke(() =>
        {
            EmbeddedStopRequested?.Invoke(this, EventArgs.Empty);
            ActivePlayerChannel = channelName;
            IsEmbeddedWebActive = false;
            IsEmbeddedVlcActive = true;
            IsEmbeddedPlaying = true;
            EmbeddedVlcPlayRequested?.Invoke(this, streamUrl);

            if (IsEmbeddedChatOpen)
                SyncEmbeddedChat(channelName);
            else if (Config.AutoOpenChat)
                OpenChat(channelName);

            UpdateDetailPlayState();
        });
    }

    [RelayCommand]
    private void SwitchEmbeddedEngine(string engine)
    {
        if (string.IsNullOrWhiteSpace(engine)) return;
        Config.PlayerChoice = engine;
        _configService.Save(Config);

        var targetChannel = !string.IsNullOrEmpty(ActivePlayerChannel)
            ? ActivePlayerChannel
            : SelectedChannel?.ChannelName;

        if (!string.IsNullOrEmpty(targetChannel) && IsEmbeddedPlaying)
        {
            if (engine.Equals("embedded_vlc", StringComparison.OrdinalIgnoreCase))
                _ = PlayEmbeddedVlcPlayerAsync(targetChannel);
            else
                PlayEmbeddedWebPlayer(targetChannel);

            UpdateDetailPlayState();
        }
    }

    [RelayCommand]
    private void ReloadEmbeddedPlayer()
    {
        if (!IsEmbeddedPlaying || string.IsNullOrEmpty(ActivePlayerChannel)) return;
        if (IsEmbeddedVlcActive)
        {
            _ = PlayEmbeddedVlcPlayerAsync(ActivePlayerChannel);
        }
        else
        {
            EmbeddedReloadRequested?.Invoke(this, EventArgs.Empty);
            StatusMessage = $"⟳ {ActivePlayerChannel.ToUpperInvariant()} oynatıcısı yenilendi.";
        }
    }

    [RelayCommand]
    private void ToggleEmbeddedMute()
    {
        if (!IsEmbeddedWebActive) return;
        IsEmbeddedMuted = !IsEmbeddedMuted;
        EmbeddedMuteRequested?.Invoke(this, IsEmbeddedMuted);
    }

    [RelayCommand]
    private void StopEmbedded()
    {
        StopEmbeddedPlayer();
        UpdateDetailPlayState();
        StatusMessage = "⏹ Yayın oynatıcı kapatıldı.";
    }

    [RelayCommand]
    private void ToggleTheaterMode()
    {
        IsTheaterMode = !IsTheaterMode;
        if (!IsTheaterMode && IsFullScreen)
        {
            SetFullScreenState(false);
        }
    }

    [RelayCommand]
    private void ToggleFullScreen()
    {
        SetFullScreenState(!IsFullScreen);
    }

    public void SetFullScreenState(bool enable)
    {
        if (IsFullScreen == enable) return;
        IsFullScreen = enable;
        if (enable)
        {
            IsTheaterMode = true;
        }
        FullScreenChanged?.Invoke(this, enable);
    }

    [RelayCommand]
    private void ToggleEmbeddedChat()
    {
        IsEmbeddedChatOpen = !IsEmbeddedChatOpen;
        Config.ShowEmbeddedChat = IsEmbeddedChatOpen;
        if (IsEmbeddedChatOpen)
        {
            var ch = !string.IsNullOrEmpty(ActivePlayerChannel) ? ActivePlayerChannel : SelectedChannel?.ChannelName;
            if (!string.IsNullOrEmpty(ch))
                SyncEmbeddedChat(ch);
        }
        else
        {
            _ = EmbeddedChat.DisconnectAsync();
        }
    }

    private async void SyncEmbeddedChat(string channelName)
    {
        if (!IsEmbeddedChatOpen || string.IsNullOrWhiteSpace(channelName)) return;
        var status = GetStatus(channelName);
        var chatroomId = status?.ChatroomId ?? 0;

        if (chatroomId == 0)
        {
            var channelData = await _api.GetChannelAsync(channelName).ConfigureAwait(false);
            chatroomId = channelData?.Chatroom?.Id ?? 0;
            if (chatroomId > 0 && status != null)
            {
                status.ChatroomId = chatroomId;
            }
        }

        if (chatroomId > 0)
        {
            await EmbeddedChat.ConnectAsync(channelName, chatroomId).ConfigureAwait(false);
        }
        else
        {
            EmbeddedChat.ChannelName = channelName;
        }
    }

    // ==================== MULTI-STREAM (SQUAD VIEW) COMMANDS ====================

    [RelayCommand]
    private void SendToMultiStream(string? channelName)
    {
        channelName ??= SelectedChannel?.ChannelName;
        if (string.IsNullOrWhiteSpace(channelName)) return;

        var existingSlot = MultiSlots.FirstOrDefault(s => s.IsActive && s.ChannelName.Equals(channelName, StringComparison.OrdinalIgnoreCase));
        if (existingSlot is null)
        {
            var targetSlot = MultiSlots.FirstOrDefault(s => !s.IsActive) ?? MultiSlots[0];
            targetSlot.ChannelName = channelName;
            StartMultiSlot(targetSlot);
        }

        ActivePage = "multistream";
        StatusMessage = $"⊞ '{channelName}' çoklu yayın ekranına eklendi.";
    }

    [RelayCommand]
    private void AutoFillLiveToMultiStream()
    {
        var liveChannels = Channels.Where(c => c.IsLive).Select(c => c.ChannelName).Take(4).ToList();
        if (liveChannels.Count == 0)
        {
            liveChannels = Channels.Select(c => c.ChannelName).Take(4).ToList();
        }

        for (int i = 0; i < MultiSlots.Count; i++)
        {
            if (i < liveChannels.Count)
            {
                MultiSlots[i].ChannelName = liveChannels[i];
                MultiSlots[i].IsMuted = i != 0;
                StartMultiSlot(MultiSlots[i]);
            }
        }
        StatusMessage = $"⊞ {liveChannels.Count} kanal çoklu yayın ızgarasına yerleştirildi.";
    }

    [RelayCommand]
    private void StartMultiSlot(MultiStreamSlotViewModel? slot)
    {
        if (slot is null || string.IsNullOrWhiteSpace(slot.ChannelName)) return;
        var clean = slot.ChannelName.Trim().TrimStart('@').ToLowerInvariant();
        slot.ChannelName = clean;
        slot.IsActive = true;
        MultiSlotPlayRequested?.Invoke(this, new MultiSlotPlayEventArgs
        {
            SlotIndex = slot.SlotIndex,
            ChannelName = clean,
            IsMuted = slot.IsMuted,
        });
    }

    [RelayCommand]
    private void StopMultiSlot(MultiStreamSlotViewModel? slot)
    {
        if (slot is null) return;
        slot.IsActive = false;
        MultiSlotStopRequested?.Invoke(this, slot.SlotIndex);
    }

    [RelayCommand]
    private void SoloAudioMultiSlot(MultiStreamSlotViewModel? targetSlot)
    {
        if (targetSlot is null) return;
        foreach (var s in MultiSlots)
        {
            s.IsMuted = s.SlotIndex != targetSlot.SlotIndex;
            if (s.IsActive)
            {
                MultiSlotMuteRequested?.Invoke(this, new MultiSlotPlayEventArgs
                {
                    SlotIndex = s.SlotIndex,
                    ChannelName = s.ChannelName,
                    IsMuted = s.IsMuted,
                });
            }
        }
        StatusMessage = $"🔊 Yalnızca #{targetSlot.SlotIndex + 1} ({targetSlot.ChannelName}) sesi aktif edildi.";
    }

    [RelayCommand]
    private void ToggleMultiLayout()
    {
        IsQuadLayout = !IsQuadLayout;
    }

    [RelayCommand]
    private void StopAllMultiStream()
    {
        foreach (var s in MultiSlots)
        {
            s.IsActive = false;
            MultiSlotStopRequested?.Invoke(this, s.SlotIndex);
        }
        StatusMessage = "⏹ Tüm çoklu yayın ekranları kapatıldı.";
    }

    // ==================== POPOUT & CHAT COMMANDS ====================

    [RelayCommand]
    private void PlayPopout(string? channelName)
    {
        channelName ??= SelectedChannel?.ChannelName;
        if (string.IsNullOrEmpty(channelName)) return;

        if (IsEmbeddedPlaying && string.Equals(ActivePlayerChannel, channelName, StringComparison.OrdinalIgnoreCase))
        {
            StopEmbeddedPlayer();
            UpdateDetailPlayState();
        }

        if (_openPlayerWindows.TryGetValue(channelName, out var existingWin) && existingWin.IsLoaded)
        {
            if (existingWin.WindowState == WindowState.Minimized)
                existingWin.WindowState = WindowState.Normal;
            existingWin.Activate();
            return;
        }

        var popout = new PlayerWindow(channelName);
        _openPlayerWindows[channelName] = popout;
        popout.Closed += (_, _) => _openPlayerWindows.Remove(channelName);
        popout.Show();
        StatusMessage = $"⛶ {channelName.ToUpperInvariant()} ayrı pencerede açıldı.";
    }

    public void StopEmbeddedPlayer()
    {
        if (IsFullScreen)
            SetFullScreenState(false);

        Streamlink.StopEmbeddedVlc();
        IsEmbeddedPlaying = false;
        IsEmbeddedVlcActive = false;
        IsEmbeddedWebActive = false;
        IsTheaterMode = false;
        ActivePlayerChannel = string.Empty;
        EmbeddedStopRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task OpenChatAsync(string? channelName)
    {
        OpenChat(channelName);
        await Task.CompletedTask;
    }

    private async void OpenChat(string? channelName)
    {
        channelName ??= SelectedChannel?.ChannelName;
        if (string.IsNullOrEmpty(channelName)) return;

        if (_openChatWindows.TryGetValue(channelName, out var existingChat) && existingChat.IsLoaded)
        {
            if (existingChat.WindowState == WindowState.Minimized)
                existingChat.WindowState = WindowState.Normal;
            existingChat.Activate();
            return;
        }

        var status = GetStatus(channelName);
        var chatroomId = status?.ChatroomId ?? 0;
        if (chatroomId == 0)
        {
            StatusMessage = $"⏳ {channelName.ToUpperInvariant()} sohbet odası bağlanıyor...";
            var channelData = await _api.GetChannelAsync(channelName).ConfigureAwait(false);
            chatroomId = channelData?.Chatroom?.Id ?? 0;
            if (chatroomId > 0 && status != null)
            {
                status.ChatroomId = chatroomId;
            }
        }

        if (chatroomId == 0)
        {
            StatusMessage = $"⚠ {channelName.ToUpperInvariant()} için sohbet odası bulunamadı.";
            return;
        }

        _dispatcher.Invoke(() =>
        {
            var chatWindow = new ChatWindow(channelName, chatroomId, Config.ChatMaxMessages);
            _openChatWindows[channelName] = chatWindow;
            chatWindow.Closed += (_, _) => _openChatWindows.Remove(channelName);
            chatWindow.Show();
        });
    }

    [RelayCommand]
    private void OpenInBrowser(string? channelName)
    {
        channelName ??= SelectedChannel?.ChannelName;
        if (string.IsNullOrEmpty(channelName)) return;
        try
        {
            Process.Start(new ProcessStartInfo($"https://kick.com/{channelName}") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Tarayıcı açılamadı: {ex.Message}";
        }
    }

    [RelayCommand]
    private void NavigateTo(string page)
    {
        if (IsFullScreen && page != "home")
            SetFullScreenState(false);
        ActivePage = page;
    }

    [RelayCommand]
    private void OpenLogs()
    {
        try
        {
            Process.Start(new ProcessStartInfo(LoggingService.Instance.LogDirectory) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Log klasörü açılamadı: {ex.Message}";
        }
    }

    [RelayCommand]
    private void TestNotificationSound()
    {
        SoundService.PlayNotification(Config.CustomSoundPath, false);
        Notifications?.ShowBalloon("🔔 LD KickWatch Bildirim Testi", "Sesli ve görsel bildirimler sorunsuz çalışıyor!");
        StatusMessage = "🔊 Bildirim sesi test edildi.";
    }

    // ==================== IMPORT / EXPORT FAVORITES ====================

    public void ExportFavoritesToFile(string filePath)
    {
        try
        {
            if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                sb.AppendLine("ChannelName,GroupName");
                foreach (var ch in Config.Favorites)
                {
                    Config.ChannelGroups.TryGetValue(ch, out var grp);
                    sb.AppendLine($"{ch},{grp ?? string.Empty}");
                }
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
            else
            {
                var exportObj = new
                {
                    ExportedAt = DateTime.Now,
                    Favorites = Config.Favorites,
                    ChannelGroups = Config.ChannelGroups,
                };
                var json = JsonSerializer.Serialize(exportObj, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            StatusMessage = $"📤 {Config.Favorites.Count} kanal başarıyla dışa aktarıldı: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠ Dışa aktarma hatası: {ex.Message}";
        }
    }

    public void ImportFavoritesFromFile(string filePath)
    {
        try
        {
            int added = 0;
            if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(',', 2);
                    if (parts.Length == 0) continue;
                    var name = parts[0].Trim().ToLowerInvariant();
                    var grp = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                    if (TryAddImportedChannel(name, grp)) added++;
                }
            }
            else
            {
                var json = File.ReadAllText(filePath, Encoding.UTF8);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var groupsDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (root.TryGetProperty("ChannelGroups", out var grpEl) && grpEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in grpEl.EnumerateObject())
                        groupsDict[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("Favorites", out var favEl) && favEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in favEl.EnumerateArray())
                    {
                        var name = item.GetString()?.Trim().ToLowerInvariant();
                        if (!string.IsNullOrEmpty(name))
                        {
                            groupsDict.TryGetValue(name, out var g);
                            if (TryAddImportedChannel(name, g ?? string.Empty)) added++;
                        }
                    }
                }
            }

            FinalizeImport(added);
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠ İçe aktarma hatası: {ex.Message}";
        }
    }

    [RelayCommand]
    private void BulkImportChannels()
    {
        if (string.IsNullOrWhiteSpace(BulkImportText)) return;

        var tokens = BulkImportText.Split(new[] { ',', ';', '\r', '\n', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        int added = 0;
        var grp = AddChannelGroupText.Trim();

        foreach (var raw in tokens)
        {
            var urlMatch = Regex.Match(raw, @"kick\.com/([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
            var name = (urlMatch.Success ? urlMatch.Groups[1].Value : raw.TrimStart('@').Trim('/')).ToLowerInvariant();
            if (TryAddImportedChannel(name, grp))
                added++;
        }

        BulkImportText = string.Empty;
        FinalizeImport(added);
    }

    private bool TryAddImportedChannel(string name, string groupName)
    {
        if (!Regex.IsMatch(name, "^[a-z0-9_-]{2,30}$")) return false;
        if (Config.Favorites.Any(f => f.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                Config.ChannelGroups[name] = groupName;
                var existing = Channels.FirstOrDefault(c => c.ChannelName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existing != null) existing.GroupName = groupName;
            }
            return false;
        }

        Config.Favorites.Add(name);
        if (!string.IsNullOrWhiteSpace(groupName))
            Config.ChannelGroups[name] = groupName;

        var card = new ChannelCardViewModel(name, groupName);
        if (_avatarCache.TryGetFreshCachedPath(name, out var cached))
            card.AvatarPath = cached;
        Channels.Add(card);
        return true;
    }

    private void FinalizeImport(int addedCount)
    {
        RebuildGroupFilterOptions();
        ApplyFilter();
        UpdateStats();
        _poller.UpdateChannelList(Config.Favorites.ToList());
        _configService.Save(Config);
        StatusMessage = $"📥 {addedCount} yeni kanal başarıyla içe aktarıldı.";
        if (addedCount > 0)
            _ = _poller.RefreshNowAsync();
    }

    // ==================== FILTER & SELECTION LOGIC ====================

    partial void OnSelectedChannelChanged(ChannelCardViewModel? value)
    {
        if (value is null)
        {
            ResetDetailPanel();
            return;
        }
        HasSelectedChannel = true;
        DetailGroupInput = value.GroupName;
        UpdateDetailPanel(value.ChannelName);

        if (IsEmbeddedChatOpen && !IsEmbeddedPlaying)
            SyncEmbeddedChat(value.ChannelName);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnOnlyLiveFilterChanged(bool value) => ApplyFilter();

    partial void OnSelectedGroupFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var q = SearchText.Trim();
        var grp = SelectedGroupFilter;
        bool filterByGroup = !string.IsNullOrWhiteSpace(grp) && !grp.Equals(AllGroupsLabel, StringComparison.OrdinalIgnoreCase);

        int count = 0;
        foreach (var card in Channels)
        {
            bool matchesSearch = string.IsNullOrEmpty(q) ||
                                 card.ChannelName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                 card.Category.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                 card.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                 card.GroupName.Contains(q, StringComparison.OrdinalIgnoreCase);
            bool matchesLive = !OnlyLiveFilter || card.IsLive;
            bool matchesGroup = !filterByGroup || card.GroupName.Equals(grp, StringComparison.OrdinalIgnoreCase);

            card.IsVisible = matchesSearch && matchesLive && matchesGroup;
            if (card.IsVisible) count++;
        }
        VisibleChannelCount = count;
    }

    private void ResetDetailPanel()
    {
        HasSelectedChannel = false;
        DetailName = "KANAL SEÇİN";
        DetailTitle = "Detayları görmek ve yayını başlatmak için sol listeden bir kanal seçin.";
        DetailCategory = string.Empty;
        DetailViewers = string.Empty;
        DetailGroupInput = string.Empty;
        DetailIsLive = false;
        DetailVerified = false;
        DetailAvatarPath = null;
        DetailIsPlaying = false;
    }

    private void UpdateDetailPanel(string channelName)
    {
        HasSelectedChannel = true;
        DetailName = channelName;
        var status = GetStatus(channelName);
        if (status is null)
        {
            DetailTitle = "Kanal bilgileri Kick.com üzerinden alınıyor...";
            DetailCategory = string.Empty;
            DetailViewers = string.Empty;
            DetailIsLive = false;
            DetailVerified = false;
        }
        else
        {
            DetailIsLive = status.IsLive;
            DetailVerified = status.Verified;
            DetailTitle = status.IsLive
                ? (string.IsNullOrWhiteSpace(status.Title) ? "Canlı Yayında" : status.Title)
                : "Bu kanal şu anda canlı yayında değil.";
            DetailCategory = status.Category;
            DetailViewers = status.IsLive
                ? (status.Viewers >= 1000 ? $"👥 {status.Viewers / 1000.0:0.0}B anlık izleyici" : $"👥 {status.Viewers} anlık izleyici")
                : "Çevrimdışı";
        }

        var card = Channels.FirstOrDefault(c => c.ChannelName.Equals(channelName, StringComparison.OrdinalIgnoreCase));
        DetailAvatarPath = card?.AvatarPath;

        UpdateDetailPlayState();
    }

    private void UpdateDetailPlayState()
    {
        bool isEmbeddedMode = string.Equals(Config.PlayerChoice, "embedded", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(Config.PlayerChoice, "embedded_vlc", StringComparison.OrdinalIgnoreCase);

        foreach (var card in Channels)
        {
            card.IsPlaying = isEmbeddedMode
                ? (IsEmbeddedPlaying && string.Equals(card.ChannelName, ActivePlayerChannel, StringComparison.OrdinalIgnoreCase))
                : Streamlink.IsPlaying(card.ChannelName);
        }

        if (isEmbeddedMode)
        {
            DetailIsPlaying = IsEmbeddedPlaying &&
                              SelectedChannel is not null &&
                              string.Equals(SelectedChannel.ChannelName, ActivePlayerChannel, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            DetailIsPlaying = SelectedChannel is not null &&
                              Streamlink.IsPlaying(SelectedChannel.ChannelName);
        }
    }

    private void OnResultsReady(object? sender, LiveStatusResultEventArgs e)
    {
        _dispatcher.Invoke(() => ApplyResults(e.Results));
    }

    private void ApplyResults(IReadOnlyDictionary<string, ChannelStatus> results)
    {
        var keywords = (Config.AlertKeywords ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim())
            .Where(k => k.Length >= 2)
            .ToList();

        foreach (var (channel, status) in results)
        {
            if (!status.FetchSucceeded) continue;

            var hadPrev = _statusCache.TryGetValue(channel, out var prev);
            var wasLive = hadPrev && prev!.IsLive;

            // 1. Yeni Canlı Yayın Bildirimi + Ses
            if (status.IsLive && !wasLive && !_notifiedLive.Contains(channel))
            {
                if (Config.NotificationsEnabled)
                {
                    Notifications?.ShowBalloon($"🔴 {channel.ToUpperInvariant()} yayına başladı!",
                        string.IsNullOrWhiteSpace(status.Category) ? status.Title : $"{status.Category} • {status.Title}");
                }
                if (Config.SoundNotificationsEnabled)
                {
                    SoundService.PlayNotification(Config.CustomSoundPath, isKeywordOrCategoryAlert: false);
                }
                _notifiedLive.Add(channel);
            }
            // 2. Kategori Değişikliği Bildirimi + Ses
            else if (status.IsLive && wasLive && Config.NotifyOnCategoryChange && prev != null &&
                     !string.IsNullOrWhiteSpace(prev.Category) &&
                     !string.IsNullOrWhiteSpace(status.Category) &&
                     !prev.Category.Equals(status.Category, StringComparison.OrdinalIgnoreCase))
            {
                if (Config.NotificationsEnabled)
                {
                    Notifications?.ShowBalloon($"🎮 {channel.ToUpperInvariant()} kategori değiştirdi!",
                        $"Yeni Kategori: {status.Category}\n{status.Title}");
                }
                if (Config.SoundNotificationsEnabled)
                {
                    SoundService.PlayNotification(Config.CustomSoundPath, isKeywordOrCategoryAlert: true);
                }
            }
            else if (!status.IsLive)
            {
                _notifiedLive.Remove(channel);
            }

            // 3. Başlık / Kategori Anahtar Kelime Uyarısı + Ses
            if (status.IsLive && keywords.Count > 0)
            {
                foreach (var kw in keywords)
                {
                    if (status.Title.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                        status.Category.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        var alertKey = $"{channel}:{kw}:{status.Title}";
                        if (_notifiedKeywords.Add(alertKey))
                        {
                            if (Config.NotificationsEnabled)
                            {
                                Notifications?.ShowBalloon($"🔔 Anahtar Kelime Yakalandı ({kw})",
                                    $"{channel.ToUpperInvariant()}: {status.Title}");
                            }
                            if (Config.SoundNotificationsEnabled)
                            {
                                SoundService.PlayNotification(Config.CustomSoundPath, isKeywordOrCategoryAlert: true);
                            }
                        }
                        break;
                    }
                }
            }

            _statusCache[channel] = status;

            var card = Channels.FirstOrDefault(c => c.ChannelName.Equals(channel, StringComparison.OrdinalIgnoreCase));
            card?.ApplyStatus(status);

            if (!string.IsNullOrWhiteSpace(status.ProfilePicUrl))
                _ = LoadAvatarAsync(channel, status.ProfilePicUrl, card);
        }

        SortChannels();
        ApplyFilter();
        UpdateStats();

        if (SelectedChannel is null && Channels.Count > 0)
            SelectedChannel = Channels[0];
        else if (SelectedChannel is not null)
            UpdateDetailPanel(SelectedChannel.ChannelName);

        // Yerleşik sohbet açıksa ve henüz bağlanmamışsa bağla
        if (IsEmbeddedChatOpen && !EmbeddedChat.IsConnected)
        {
            var chatTarget = !string.IsNullOrEmpty(ActivePlayerChannel) ? ActivePlayerChannel : SelectedChannel?.ChannelName;
            if (!string.IsNullOrEmpty(chatTarget))
                SyncEmbeddedChat(chatTarget);
        }
    }

    private void SortChannels()
    {
        var sorted = Channels
            .OrderByDescending(c => c.IsLive)
            .ThenByDescending(c => c.Viewers)
            .ThenBy(c => c.ChannelName)
            .ToList();

        bool orderChanged = false;
        for (int i = 0; i < sorted.Count; i++)
        {
            if (!ReferenceEquals(Channels[i], sorted[i]))
            {
                orderChanged = true;
                break;
            }
        }

        if (!orderChanged) return;

        for (int i = 0; i < sorted.Count; i++)
        {
            var currentIndex = Channels.IndexOf(sorted[i]);
            if (currentIndex != i)
                Channels.Move(currentIndex, i);
        }
    }

    private void UpdateStats()
    {
        TotalChannels = Channels.Count;
        LiveCount = _statusCache.Values.Count(s => s.IsLive);
        TotalViewers = _statusCache.Values.Where(s => s.IsLive).Sum(s => s.Viewers);
    }

    private async Task LoadAvatarAsync(string channel, string url, ChannelCardViewModel? card)
    {
        if (card is null) return;
        if (_avatarCache.TryGetFreshCachedPath(channel, out var cachedPath))
        {
            _dispatcher.Invoke(() =>
            {
                card.AvatarPath = cachedPath;
                if (SelectedChannel?.ChannelName.Equals(channel, StringComparison.OrdinalIgnoreCase) == true)
                    DetailAvatarPath = cachedPath;
            });
            return;
        }
        var path = await _avatarCache.DownloadAndCacheAsync(channel, url);
        if (path != null)
        {
            _dispatcher.Invoke(() =>
            {
                card.AvatarPath = path;
                if (SelectedChannel?.ChannelName.Equals(channel, StringComparison.OrdinalIgnoreCase) == true)
                    DetailAvatarPath = path;
            });
        }
    }

    public ChannelStatus? GetStatus(string channel) =>
        _statusCache.TryGetValue(channel, out var s) ? s : null;
}
