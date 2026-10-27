using System.Collections.ObjectModel;

namespace KickStreamHub.Models;

public class AppConfig
{
    public ObservableCollection<string> Favorites { get; set; } = new()
    {
        "elwind", "wtcn", "pqueen", "unlostv", "kendinemuzisyen"
    };

    public Dictionary<string, string> ChannelGroups { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string PlayerChoice { get; set; } = "embedded";
    public int RefreshIntervalSeconds { get; set; } = 90;
    public bool StartMinimized { get; set; } = false;
    public bool MinimizeToTray { get; set; } = false;
    public bool NotificationsEnabled { get; set; } = true;
    public bool SoundNotificationsEnabled { get; set; } = true;
    public string CustomSoundPath { get; set; } = string.Empty;
    public bool NotifyOnCategoryChange { get; set; } = true;
    public string AlertKeywords { get; set; } = string.Empty;
    public bool RunOnStartup { get; set; } = false;
    public bool AutoOpenChat { get; set; } = false;
    public bool ShowEmbeddedChat { get; set; } = true;

    public double WindowWidth { get; set; } = 1320;
    public double WindowHeight { get; set; } = 800;
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;

    public string StreamlinkPath { get; set; } = string.Empty;
    public string PlayerPath { get; set; } = string.Empty;
    public string StreamQuality { get; set; } = "best";
    public string ExtraStreamlinkArgs { get; set; } = string.Empty;

    public string ActivePage { get; set; } = "home";
    public int ChatMaxMessages { get; set; } = 500;
}
