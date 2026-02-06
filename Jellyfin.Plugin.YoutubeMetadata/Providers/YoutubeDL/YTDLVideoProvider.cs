using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller;
using System.Net.Http;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YTDLVideoProvider : AbstractYoutubeRemoteProvider<YTDLVideoProvider, Video, ItemLookupInfo>
    {
        public YTDLVideoProvider(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            ILogger<YTDLVideoProvider> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, httpClientFactory, logger, config, afs)
        {
        }

        public override string Name => Constants.PluginName;

        internal override MetadataResult<Video> GetMetadataImpl(YTDLData jsonObj, string id) => YTDLJsonToVideo(jsonObj, id);

        internal async override Task GetAndCacheMetadata(
            string id,
            IServerApplicationPaths appPaths,
            CancellationToken cancellationToken) => await Utils.YTDLMetadata(id, appPaths, cancellationToken);
    }
}
