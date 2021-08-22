using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class CreatorProviderImageProvider : IRemoteImageProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClientFactory _httpClientFactory;

        public CreatorProviderImageProvider(IServerConfigurationManager config, IFileSystem fileSystem, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _fileSystem = fileSystem;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Returns true if BaseItem is of type that this provider supports.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Supports(BaseItem item)
        {
            return item is Person;
        }

        /// <summary>
        /// Providers name, this does not appear in the library metadata settings.
        /// </summary>
        public string Name => "YouTube Creator Metadata";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <summary>
        /// Retrieves metadata for a Creator.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {

            var infos = new List<RemoteImageInfo>();
            var channelId = item.ProviderIds["youtubemetadata"];
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(channelId))
            {
                await EnsureInfo(channelId, cancellationToken).ConfigureAwait(false);

                var path = Utils.GetVideoInfoPath(_config.ApplicationPaths, channelId);
                string jsonString = File.ReadAllText(path);
                var channel = JsonSerializer.Deserialize<Google.Apis.YouTube.v3.Data.Channel>(jsonString);
                if (channel != null)
                {
                    var rii = new RemoteImageInfo();
                    if (channel.Snippet.Thumbnails.Maxres != null)
                    {
                        rii.Url = channel.Snippet.Thumbnails.Maxres.Url;
                    }
                    else if (channel.Snippet.Thumbnails.Standard != null)
                    {
                        rii.Url = channel.Snippet.Thumbnails.Standard.Url;
                    }
                    else if (channel.Snippet.Thumbnails.High != null)
                    {
                        rii.Url = channel.Snippet.Thumbnails.High.Url;
                    }
                    else if (channel.Snippet.Thumbnails.Medium != null)
                    {
                        rii.Url = channel.Snippet.Thumbnails.Medium.Url;
                    }
                    else if (channel.Snippet.Thumbnails.Default__.Url != null)
                    {
                        rii.Url = channel.Snippet.Thumbnails.Default__.Url;
                    }
                    infos.Add(rii);
                }
            }
            return infos;
        }

        /// <summary>
        /// Ensures that metadata is fresh in the cache or retrieves fresh data.
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal Task EnsureInfo(string channelId, CancellationToken cancellationToken)
        {
            var ytPath = Utils.GetVideoInfoPath(_config.ApplicationPaths, channelId);

            var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
            if (Utils.IsFresh(fileInfo))
            {
                return Task.CompletedTask;
            }
            return Utils.APIDownload(channelId, _config.ApplicationPaths, Utils.DownloadType.Channel, cancellationToken);
        }

        /// <summary>
        /// Gets the image response.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            return await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        }
    }
}
