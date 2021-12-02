using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers.YoutubeDL
{
    public class YoutubeDLEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<YoutubeMetadataProvider> _logger;

        public const string BaseUrl = "https://m.youtube.com/";

        public YoutubeDLEpisodeProvider(IServerConfigurationManager config, IFileSystem fileSystem, ILogger<YoutubeMetadataProvider> logger)
        {
            _config = config;
            _fileSystem = fileSystem;
            _logger = logger;
        }
        public string Name => "YouTube-DL Episode Metadata";

        public int Order => 1;

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            Console.WriteLine("Inside GetMetadata");
            var result = new MetadataResult<Episode>();

            var id = Utils.GetYTID(info.Path);
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogInformation("Youtube ID not found in filename of title: " + info.Name);
                return result;
            }
            var ytPath = Utils.GetVideoInfoPath(_config.ApplicationPaths, id);
            var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
            if (!(Utils.IsFresh(fileInfo)))
            {
                await Utils.YTDLMetadata(id, _config.ApplicationPaths, cancellationToken);
            }
            var path = Utils.GetVideoInfoPath(_config.ApplicationPaths, id);
            var video = Utils.ReadYTDLInfo(path, cancellationToken);
            if (video != null)
            {
                result = Utils.YTDLJsonToEpisode(video);
            }

            return result;
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
