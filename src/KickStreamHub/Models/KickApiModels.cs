using System.Text.Json.Serialization;

namespace KickStreamHub.Models;

public class KickChannelResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    [JsonPropertyName("playback_url")]
    public string? PlaybackUrl { get; set; }

    [JsonPropertyName("user")]
    public KickUser? User { get; set; }

    [JsonPropertyName("livestream")]
    public KickLivestream? Livestream { get; set; }

    [JsonPropertyName("chatroom")]
    public KickChatroom? Chatroom { get; set; }
}

public class KickUser
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("profile_pic")]
    public string? ProfilePic { get; set; }
}

public class KickLivestream
{
    [JsonPropertyName("session_title")]
    public string? SessionTitle { get; set; }

    [JsonPropertyName("viewer_count")]
    public long ViewerCount { get; set; }

    [JsonPropertyName("categories")]
    public List<KickCategory>? Categories { get; set; }
}

public class KickCategory
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class KickChatroom
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
