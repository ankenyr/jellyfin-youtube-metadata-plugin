using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller;
using Google.Apis.YouTube.v3.Data;
using System;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers.YT_API
{
    public class YTAPIMovieProvider : AbstractYTAPIProvider<YTAPIMovieProvider, Movie, MovieInfo, Video>
    {
        //private readonly IServerConfigurationManager _config;
        public YTAPIMovieProvider(
            IFileSystem fileSystem,
            ILogger<YTAPIMovieProvider> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, logger, config, afs)
        {
        }

        public override string Name => "YouTube-API Movie Metadata";

        internal override MetadataResult<Movie> GetMetadataImpl(YTData jsonObj) => YTDLJsonToMovie(jsonObj);

        internal async override Task GetAndCacheMetadata(string id, IServerApplicationPaths appPaths, CancellationToken cancellationToken)
        {
            await APIDownload(id, appPaths, cancellationToken);
        }
    }
}