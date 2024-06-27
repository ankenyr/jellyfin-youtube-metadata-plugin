using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.YoutubeMetadata;

/// <summary>
/// Represents the data retrieved from YouTube-DL for a YouTube video.
/// </summary>
public sealed class YTVideoDto
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
    /// Gets or sets the upload date of the YouTube video.
    /// </summary>
    [JsonPropertyName("upload_date")]
    public required string UploadDate { get; set; }

    /// <summary>
    /// Gets or sets the title of the YouTube video.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }

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

    /// <summary>
    /// Gets or sets the playlist index of the YouTube video.
    /// </summary>
    [JsonPropertyName("playlist_index")]
    public required int PlaylistIndex { get; set; }
}
