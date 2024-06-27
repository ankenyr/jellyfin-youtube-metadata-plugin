using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.YoutubeMetadata;

/// <summary>
/// Represents the data retrieved from YouTube-DL for a YouTube video.
/// </summary>
public sealed class YTChannelDto
{
    /// <summary>
    /// Gets or sets the ID of the YouTube video.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the uploader of the YouTube video.
    /// </summary>
    [JsonPropertyName("uploader")]
    public required string Uploader { get; set; }

    /// <summary>
    /// Gets or sets the description of the YouTube video.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the channel ID of the YouTube video.
    /// </summary>
    [JsonPropertyName("channel_id")]
    public required string ChannelId { get; set; }
}
