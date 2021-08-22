using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YoutubeMusicProvider : IRemoteMetadataProvider<MusicVideo, MusicVideoInfo>, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<YoutubeMusicProvider> _logger;

        public const string BaseUrl = "https://m.youtube.com/";

        public YoutubeMusicProvider(IServerConfigurationManager config, IFileSystem fileSystem, ILogger<YoutubeMusicProvider> logger)
        {
            _config = config;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <summary>
        /// Providers name, this appears in the library metadata settings.
        /// </summary>
        public string Name => "YouTube Music Metadata";

        /// <inheritdoc />
        public int Order => 1;

        public async Task<MetadataResult<MusicVideo>> GetMetadata(MusicVideoInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MusicVideo>();
            var id = Utils.GetYTID(info.Name);

            _logger.LogInformation(id);

            if (!string.IsNullOrWhiteSpace(id))
            {
                var ytPath = Utils.GetVideoInfoPath(_config.ApplicationPaths, id);
                var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
                if (Utils.IsFresh(fileInfo))
                {
                    return result;
                }
                await Utils.APIDownload(id, _config.ApplicationPaths, Utils.DownloadType.Video, cancellationToken);

                var path = Utils.GetVideoInfoPath(_config.ApplicationPaths, id);
                string jsonString = File.ReadAllText(path);
                var video = JsonSerializer.Deserialize<Google.Apis.YouTube.v3.Data.Video>(jsonString);
                if (video != null)
                {
                    result.Item = new MusicVideo();
                    result.HasMetadata = true;
                    result.Item.OriginalTitle = info.Name;
                    YoutubeMetadataProvider.ProcessResult(result.Item, video);
                    result.AddPerson(Utils.CreatePerson(video.Snippet.ChannelTitle, video.Snippet.ChannelId));
                }
            }
            else
            {
                _logger.LogInformation("Youtube ID not found in filename of title: " + info.Name);
            }

            return result;
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MusicVideoInfo searchInfo, CancellationToken cancellationToken)
            => Task.FromResult(Enumerable.Empty<RemoteSearchResult>());

        /// <summary>
        /// Gets the supported images.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

}
