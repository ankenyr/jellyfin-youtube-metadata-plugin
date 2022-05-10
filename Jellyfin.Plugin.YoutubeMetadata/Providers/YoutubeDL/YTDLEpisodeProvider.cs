using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller;
using System.Net.Http;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YTDLEpisodeProvider: AbstractYoutubeRemoteProvider<YTDLEpisodeProvider, Episode, EpisodeInfo>
    {
        public YTDLEpisodeProvider(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            ILogger<YTDLEpisodeProvider> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, httpClientFactory, logger, config, afs)
        {
        }

        public override string Name => Constants.PluginName;

        internal override MetadataResult<Episode> GetMetadataImpl(YTDLData jsonObj, string id) => YTDLJsonToEpisode(jsonObj, id);

        internal async override Task GetAndCacheMetadata(
            string id,
            IServerApplicationPaths appPaths,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("YTDLEpisodeProvider: GetAndCacheMetadata ", id);
            await Utils.YTDLMetadata(id, appPaths, cancellationToken);
        }
    }
}