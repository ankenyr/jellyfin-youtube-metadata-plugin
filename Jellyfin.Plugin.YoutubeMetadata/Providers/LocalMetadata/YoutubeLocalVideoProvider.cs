using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YoutubeLocalVideoProvider : AbstractYoutubeLocalProvider<YoutubeLocalVideoProvider, Video>
    {
        public YoutubeLocalVideoProvider(IFileSystem fileSystem, ILogger<YoutubeLocalVideoProvider> logger) : base(fileSystem, logger) { }

        public override string Name => Constants.PluginName;

        internal override MetadataResult<Video> GetMetadataImpl(YTDLData jsonObj)
        {
            return Utils.YTDLJsonToVideo(jsonObj);
        }
    }
}
