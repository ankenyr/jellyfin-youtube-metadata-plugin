using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.IO;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class CreatorProviderImageProvider : AbstractYoutubeImageRemoteProvider<YTAPIImageProvider>
    {

        public CreatorProviderImageProvider(
            IServerConfigurationManager config,
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            ILogger<YTAPIImageProvider> logger,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, logger, config, httpClientFactory, afs)
        { }

        /// <summary>
        /// Providers name, this appears in the library metadata settings.
        /// </summary>
        public override string Name => Constants.PluginName;

        /// <summary>
        /// Returns true if BaseItem is of type that this provider supports.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool Supports(BaseItem item)
        {
            return item is Person;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }
        /// <summary>
        /// Retrieves image for item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            List<RemoteImageInfo> infos = new();
            var channelId = item.ProviderIds["youtubemetadata"];
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(channelId))
            {
                var path = Utils.GetVideoInfoPath(_config.ApplicationPaths, channelId);
                string jsonString = File.ReadAllText(path);
                var channel = JsonSerializer.Deserialize<Google.Apis.YouTube.v3.Data.Channel>(jsonString);
                if (channel != null)
                {
                    var rii = new RemoteImageInfo
                    {
                        ProviderName = Name,
                        Type = ImageType.Primary
                    };
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

            
           
            return await Task.FromResult(infos);
        }
    }
}
