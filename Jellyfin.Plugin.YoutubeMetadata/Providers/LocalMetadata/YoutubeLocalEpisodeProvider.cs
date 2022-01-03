using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YoutubeLocalEpisodeProvider : AbstractYoutubeLocalProvider<YoutubeLocalEpisodeProvider, Episode>
    {
        public YoutubeLocalEpisodeProvider(IFileSystem fileSystem, ILogger<YoutubeLocalEpisodeProvider> logger) : base(fileSystem, logger) { }

        public override string Name => Constants.PluginName;

        internal override MetadataResult<Episode> GetMetadataImpl(Utils.YTDLMovieJson jsonObj)
        {
            return Utils.YTDLJsonToEpisode(jsonObj);
        }
    }
}
