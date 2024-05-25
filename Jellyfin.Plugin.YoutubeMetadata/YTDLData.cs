using System.Collections.Generic;

namespace Jellyfin.Plugin.YoutubeMetadata
{
    /// <summary>
    /// Object should match how YTDL json looks.
    /// </summary>
#pragma warning disable IDE1006 // Naming Styles
    public class ThumbnailInfo
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string resolution { get; set; }
        public string id { get; set; }
    }
    public class YTDLData
    {
        public string id { get; set; }
        // Human name
        public string uploader { get; set; }
        public string upload_date { get; set; }
        // https://github.com/ytdl-org/youtube-dl/issues/1806
        public string title { get; set; }
        public string description { get; set; }
        // Name for use in API?
        public string channel_id { get; set; }
        public string track { get; set; }
        public string artist { get; set; }
        public string album { get; set; }
        public List<ThumbnailInfo> thumbnails { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }
}