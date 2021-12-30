using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using Google.Apis.YouTube.v3.Data;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YTAPIMusicProvider : AbstractYTAPIProvider<YTAPIMusicProvider, MusicVideo, MusicVideoInfo, Google.Apis.YouTube.v3.Data.Video>
    {
        //private readonly IServerConfigurationManager _config;
        public YTAPIMusicProvider(
            IFileSystem fileSystem,
            ILogger<YTAPIMusicProvider> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, logger, config, afs)
        {
        }

        public override string Name => Constants.PluginName;

        internal override MetadataResult<MusicVideo> GetMetadataImpl(YTDLData jsonObj, string id) => YTDLJsonToMusicVideo(jsonObj, id);

        internal async override Task GetAndCacheMetadata(string id, IServerApplicationPaths appPaths, CancellationToken cancellationToken)
        {
            await APIDownload(id, _config.ApplicationPaths, cancellationToken);
        }
    }
}