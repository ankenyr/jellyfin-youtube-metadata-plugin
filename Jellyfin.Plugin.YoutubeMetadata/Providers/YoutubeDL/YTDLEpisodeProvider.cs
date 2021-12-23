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
            ILogger<YTDLEpisodeProvider> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, logger, config, afs)
        {
        }

        public override string Name => "YouTube-DL Episode Metadata";

        internal override MetadataResult<Episode> GetMetadataImpl(YTData jsonObj) => YTDLJsonToEpisode(jsonObj);

        internal async override Task GetAndCacheMetadata(
            string id,
            IServerApplicationPaths appPaths,
            CancellationToken cancellationToken) => await Utils.YTDLMetadata(id, appPaths, cancellationToken);
    }
}