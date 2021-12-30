using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller;
using System.IO;
using System.Text.Json;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YTDLEpisodeProvider: AbstractYoutubeRemoteProvider<YTDLEpisodeProvider, Episode, EpisodeInfo>
    {
        public YTDLEpisodeProvider(
            IFileSystem fileSystem,
            ILogger<YTDLEpisodeProvider> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, logger, config, afs)
        {
        }

        public override string Name => Constants.PluginName;

        internal override MetadataResult<Episode> GetMetadataImpl(YTDLData jsonObj) => YTDLJsonToEpisode(jsonObj);

        internal async override Task GetAndCacheMetadata(
            string id,
            IServerApplicationPaths appPaths,
            CancellationToken cancellationToken)
        {
            await Utils.YTDLMetadata(id, appPaths, cancellationToken);
        }
    }
}