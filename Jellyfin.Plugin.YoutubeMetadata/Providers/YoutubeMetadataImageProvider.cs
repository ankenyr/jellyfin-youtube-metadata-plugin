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

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
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
