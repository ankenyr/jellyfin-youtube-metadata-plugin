using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YoutubeLocalMovieProvider : AbstractYoutubeLocalProvider<YoutubeLocalMovieProvider, Movie>
    {
        public YoutubeLocalMovieProvider(IFileSystem fileSystem, ILogger<YoutubeLocalMovieProvider> logger) : base(fileSystem, logger) { }

        public override string Name => Constants.PluginName;

        internal override MetadataResult<Movie> GetMetadataImpl(Utils.YTDLMovieJson jsonObj)
        {
            return Utils.YTDLJsonToMovie(jsonObj);
        }
    }
}
