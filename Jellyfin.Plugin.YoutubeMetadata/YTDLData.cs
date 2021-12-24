

namespace Jellyfin.Plugin.YoutubeMetadata
{
    /// <summary>
    /// Object should match how YTDL json looks.
    /// </summary>
    public class YTDLData
    {
        #pragma warning disable IDE1006 // Naming Styles
        // Human name
        public string uploader { get; set; }
        public string upload_date { get; set; }
        public string uploader_id { get; set; }
        // https://github.com/ytdl-org/youtube-dl/issues/1806
        public string title { get; set; }
        public string description { get; set; }
        // Name for use in API?
        public string channel_id { get; set; }
        public string track { get; set; }
        public string artist { get; set; }
        public string album { get; set; }
        public string thumbnail { get; set; }
        #pragma warning restore IDE1006 // Naming Styles
    }
}
