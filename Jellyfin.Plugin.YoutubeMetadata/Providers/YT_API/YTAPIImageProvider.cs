using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.IO;
using System.Linq;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YTAPIImageProvider : AbstractYoutubeImageRemoteProvider<YTAPIImageProvider>
    {

        public YTAPIImageProvider(
            IServerConfigurationManager config,
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            ILogger<YTAPIImageProvider> logger,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, logger, config, httpClientFactory, afs)
        { }

        /// <summary>
        /// Providers name, this appears in the library metadata settings.
        /// </summary>
        public override string Name => "YouTube API Image Metadata";

        /// <summary>
        /// Gets the supported images.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Disc
            };
        }

        /// <summary>
        /// Retrieves image for item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var tnurls = new List<RemoteImageInfo>();
            var id = Utils.GetYTID(item.FileNameWithoutExtension);
            if (string.IsNullOrWhiteSpace(id))
            {
                return tnurls;
            }
            var ytPath = Utils.GetVideoInfoPath(_config.ApplicationPaths, id);
            var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
            if (!YTAPIMovieProvider.IsFresh(fileInfo))
            {
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        .AddFilter("Microsoft", LogLevel.Warning)
                        .AddFilter("System", LogLevel.Warning)
                        .AddFilter("SampleApp.Program", LogLevel.Debug);
                });
                YTAPIMovieProvider downloader = new(this._fileSystem, loggerFactory.CreateLogger<YTAPIMovieProvider>(), this._config, this._afs);
                await downloader.GetAndCacheMetadata(id, this._config.ApplicationPaths, cancellationToken);
            }
            string jsonString = File.ReadAllText(ytPath);
            var obj = JsonSerializer.Deserialize<YTData>(jsonString);

            if (obj == null)
            {
                return tnurls;
            }
            
            var url = new RemoteImageInfo
            {
                ProviderName = Name,
                Url = obj.thumbnail,
                Type = ImageType.Primary
            };
            return tnurls.Append(url);
        }

        /// <summary>
        /// Returns True if item is supported.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool Supports(BaseItem item)
            => item is Movie;
    }
}
