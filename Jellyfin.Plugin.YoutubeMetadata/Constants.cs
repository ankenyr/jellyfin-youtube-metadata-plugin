namespace Jellyfin.Plugin.YoutubeMetadata
{
    class Constants
    {
        public const string PluginName = "YoutubeMetadata";
        public const string PluginGuid = "b4b4353e-dc57-4398-82c1-de9079e7146a";
        public const string ChannelUrl = "https://www.youtube.com/channel/{0}";
        public const string SearchQuery = "https://www.youtube.com/results?search_query={0}&sp=EgIQAg%253D%253D";

        public const string YTCHANNEL_RE = @"(?<=\/)[a-zA-Z0-9\-_]{24}";
    }
}
