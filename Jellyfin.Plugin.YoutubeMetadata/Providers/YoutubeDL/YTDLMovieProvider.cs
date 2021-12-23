using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YTDLMovieProvider : AbstractYoutubeRemoteProvider<YTDLMovieProvider, Movie, MovieInfo>
    {
        public YTDLMovieProvider(
            IFileSystem fileSystem,
            ILogger<YTDLMovieProvider> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, logger, config, afs)
        {
        }

        public override string Name => "YouTube-DL Movie Metadata";

        internal override MetadataResult<Movie> GetMetadataImpl(YTData jsonObj) => YTDLJsonToMovie(jsonObj);

        internal async override Task GetAndCacheMetadata(
            string id,
            IServerApplicationPaths appPaths,
            CancellationToken cancellationToken) => await Utils.YTDLMetadata(id, appPaths, cancellationToken);
    }
}