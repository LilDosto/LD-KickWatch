using CommunityToolkit.Mvvm.ComponentModel;
using KickStreamHub.Models;

namespace KickStreamHub.ViewModels;

public partial class ChannelCardViewModel : ObservableObject
{
    [ObservableProperty] private string _channelName;
    [ObservableProperty] private string _groupName = string.Empty;
    [ObservableProperty] private bool _isLive;
    [ObservableProperty] private string _title = "Durum kontrol ediliyor...";
    [ObservableProperty] private string _category = string.Empty;
    [ObservableProperty] private long _viewers;
    [ObservableProperty] private bool _verified;
    [ObservableProperty] private string? _avatarPath;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private bool _isPlaying;

    public ChannelCardViewModel(string channelName, string groupName = "")
    {
        _channelName = channelName;
        _groupName = groupName;
    }

    public void ApplyStatus(ChannelStatus status)
    {
        IsLive = status.IsLive;
        Title = status.IsLive
            ? (string.IsNullOrWhiteSpace(status.Title) ? "Canlı Yayında" : status.Title)
            : "Şu anda çevrimdışı";
        Category = status.Category;
        Viewers = status.Viewers;
        Verified = status.Verified;
        OnPropertyChanged(nameof(ViewersDisplay));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(SubText));
    }

    public string ViewersDisplay => Viewers >= 1000 ? $"{Viewers / 1000.0:0.0}B" : Viewers.ToString();
    public string StatusText => IsLive ? "● CANLI" : "KAPALI";
    public string SubText => IsLive
        ? $"{(string.IsNullOrEmpty(Category) ? "Canlı Yayın" : $"🎮 {Category}")}  •  👥 {ViewersDisplay} izleyici"
        : "Yayında değil";
}
