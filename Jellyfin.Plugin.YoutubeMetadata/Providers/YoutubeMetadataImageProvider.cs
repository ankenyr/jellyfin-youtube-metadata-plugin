using System.Collections.Generic;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YoutubeMetadataImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJsonSerializer _json;
        private readonly ILogger<YoutubeMetadataImageProvider> _logger;
        public static YoutubeMetadataProvider Current;

        public YoutubeMetadataImageProvider(IServerConfigurationManager config, IHttpClientFactory httpClientFactory, IJsonSerializer json, ILogger<YoutubeMetadataImageProvider> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _json = json;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "YouTube Metadata";

        /// <inheritdoc />
        // After embedded and fanart
        public int Order => 1;

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
            var id = YoutubeMetadataProvider.GetYTID(item.FileNameWithoutExtension);

            if (!string.IsNullOrWhiteSpace(id))
            {
                await YoutubeMetadataProvider.Current.EnsureInfo(id, cancellationToken).ConfigureAwait(false);

                var path = YoutubeMetadataProvider.GetVideoInfoPath(_config.ApplicationPaths, id);

                var obj = _json.DeserializeFromFile<Google.Apis.YouTube.v3.Data.Video>(path);

                if (obj != null)
                {
                    var tnurls = new List<string>();
                    if (obj.Snippet.Thumbnails.Maxres != null) {
                        tnurls.Add(obj.Snippet.Thumbnails.Maxres.Url);
                    }
                    if (obj.Snippet.Thumbnails.Standard != null)
                    {
                        tnurls.Add(obj.Snippet.Thumbnails.Standard.Url);
                    }
                    if (obj.Snippet.Thumbnails.High != null)
                    {
                        tnurls.Add(obj.Snippet.Thumbnails.High.Url);
                    }
                    if (obj.Snippet.Thumbnails.Medium != null)
                    {
                        tnurls.Add(obj.Snippet.Thumbnails.Medium.Url);
                    }
                    if (obj.Snippet.Thumbnails.Default__.Url != null)
                    {
                        tnurls.Add(obj.Snippet.Thumbnails.Default__.Url);
                    }

                    return GetImages(tnurls);
                }
                else
                {
                    _logger.LogInformation("Object is null!");
                }
            }

            return new List<RemoteImageInfo>();
        }

        private IEnumerable<RemoteImageInfo> GetImages(IEnumerable<string> urls)
        {
            var list = new List<RemoteImageInfo>();
            foreach (string url in urls)
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    list.Add(new RemoteImageInfo
                    {
                        ProviderName = Name,
                        Url = url,
                        Type = ImageType.Primary
                    });
                }
            }
            return list;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            return httpClient.GetAsync(url);
        }

        /// <inheritdoc />
        public bool Supports(BaseItem item)
            => item is Movie;
    }
}
