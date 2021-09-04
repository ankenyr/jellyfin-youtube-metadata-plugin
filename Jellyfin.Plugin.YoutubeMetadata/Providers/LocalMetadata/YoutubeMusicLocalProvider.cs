using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YoutubeMusicLocalProvider : AbstractYoutubeLocalProvider<YoutubeMusicLocalProvider, MusicVideo>
    {
        public YoutubeMusicLocalProvider(IFileSystem fileSystem, ILogger<YoutubeMusicLocalProvider> logger) : base(fileSystem, logger) { }

        public override string Name => "YouTube Music Local Metadata";

        internal override MetadataResult<MusicVideo> GetMetadataImpl(Utils.YTDLMovieJson jsonObj)
        {
            return Utils.YTDLJsonToMusicVideo(jsonObj);
        }
    }
}
