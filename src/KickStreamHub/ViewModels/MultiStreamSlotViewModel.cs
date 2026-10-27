using CommunityToolkit.Mvvm.ComponentModel;

namespace KickStreamHub.ViewModels;

public partial class MultiStreamSlotViewModel : ObservableObject
{
    public int SlotIndex { get; init; }
    public string SlotTitle => $"Ekran #{SlotIndex + 1}";

    [ObservableProperty] private string _channelName = string.Empty;
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private bool _isMuted = true;
}
