using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YoutubeMetadata
{
    public class YoutubeMetadataImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;
        private readonly ILogger _logger;
        public static YoutubeMetadataProvider Current;

        public YoutubeMetadataImageProvider(IServerConfigurationManager config, IHttpClient httpClient, IJsonSerializer json, ILogger logger)
        {
            _config = config;
            _httpClient = httpClient;
            _json = json;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "YoutubeMetadata";

        /// <inheritdoc />
        // After embedded and fanart
        public int Order => 2;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Disc
            };
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var id = YoutubeMetadataProvider.Current.GetYTID(item.Name);

            if (!string.IsNullOrWhiteSpace(id))
            {
                await YoutubeMetadataProvider.Current.EnsureInfo(id, cancellationToken).ConfigureAwait(false);

                var path = YoutubeMetadataProvider.GetVideoInfoPath(_config.ApplicationPaths, id);

                var obj = _json.DeserializeFromFile<Google.Apis.YouTube.v3.Data.Video>(path);

                if (obj != null)
                {
                    return GetImages(obj.Snippet.Thumbnails.Maxres.Url);
                }
            }

            return new List<RemoteImageInfo>();
        }

        private IEnumerable<RemoteImageInfo> GetImages(string url)
        {
            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrWhiteSpace(url))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = url,
                    Type = ImageType.Primary
                });
            }

            return list;
        }

        /// <inheritdoc />
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }

        /// <inheritdoc />
        public bool Supports(BaseItem item)
            => item is Movie;
    }
}
