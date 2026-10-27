namespace KickStreamHub.Models;

public class ChannelStatus
{
    public string ChannelName { get; set; } = string.Empty;
    public bool IsLive { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long Viewers { get; set; }
    public bool Verified { get; set; }
    public string ProfilePicUrl { get; set; } = string.Empty;
    public string PlaybackUrl { get; set; } = string.Empty;
    public long? ChatroomId { get; set; }
    public bool FetchSucceeded { get; set; } = true;
    public DateTime FetchedAt { get; set; } = DateTime.Now;
}
