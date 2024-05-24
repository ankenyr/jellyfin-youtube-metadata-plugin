using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers.ExternalId
{
    public class YTVideoExternalId : IExternalId
    {
        public bool Supports(IHasProviderIds item)
            => item is Movie || item is Episode || item is MusicVideo;

        public string ProviderName
            => "YouTube";

        public string Key
            => Constants.PluginName;

        public ExternalIdMediaType? Type
            => null;

        public string UrlFormatString
            => Constants.VideoUrl;
    }

    public class YTSeriesExternalId : IExternalId
    {
        public bool Supports(IHasProviderIds item)
            => item is Series;

        public string ProviderName
            => "YouTube";

        public string Key
            => Constants.PluginName;

        public ExternalIdMediaType? Type
            => null;

        public string UrlFormatString
            => Constants.ChannelUrl;
    }
}
