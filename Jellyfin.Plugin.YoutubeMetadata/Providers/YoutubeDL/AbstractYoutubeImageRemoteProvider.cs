using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public abstract class AbstractYoutubeImageRemoteProvider<B> : IRemoteImageProvider, IHasOrder
    {
        protected readonly IServerConfigurationManager _config;
        protected readonly ILogger<B> _logger;
        protected readonly IFileSystem _fileSystem;
        private readonly IHttpClientFactory _httpClientFactory;
        protected readonly System.IO.Abstractions.IFileSystem _afs;
        public AbstractYoutubeImageRemoteProvider(IFileSystem fileSystem,
            ILogger<B> logger,
            IServerConfigurationManager config,
            IHttpClientFactory httpClientFactory,
            System.IO.Abstractions.IFileSystem afs)
        {
            _config = config;
            _fileSystem = fileSystem;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _afs = afs;
        }
        public int Order => 1;
        public abstract string Name { get; }

        /// <summary>
        /// Gets the image response.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            _logger.LogDebug("YTDLRemoteImage GetImageResponse: {URL}", url);
            var httpClient = _httpClientFactory.CreateClient();
            return await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        }
        public abstract Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken);
        public abstract IEnumerable<ImageType> GetSupportedImages(BaseItem item);
        public abstract bool Supports(BaseItem item);
    }
}
