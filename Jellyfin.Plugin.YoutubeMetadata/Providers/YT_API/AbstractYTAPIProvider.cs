using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MediaBrowser.Controller;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Requests;


namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public abstract class AbstractYTAPIProvider <B, T, E, D> : AbstractYoutubeRemoteProvider<B, T, E>
        where T : BaseItem, IHasLookupInfo<E>
        where E : ItemLookupInfo, new()
        where D : IDirectResponseSchema, new()
    {
        protected readonly IServerConfigurationManager _config;
        protected readonly ILogger<B> _logger;
        protected readonly IFileSystem _fileSystem;

        public AbstractYTAPIProvider(IFileSystem fileSystem,
            ILogger<B> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, logger, config, afs)
        {
        }
        /// <summary>
        /// Returns a json of the videos metadata.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static async Task<string> Download(string id, VideosResource resource)
        {
            var vreq = resource.List("snippet");
            vreq.Id = id;
            var response = await vreq.ExecuteAsync();
            return JsonSerializer.Serialize(response.Items[0]);
        }

        /// <summary>
        /// Returns a json of the channels metadata.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        private static async Task<string> Download(string id, ChannelsResource resource)
        {
            var vreq = resource.List("snippet");
            vreq.Id = id;
            var response = await vreq.ExecuteAsync();
            return JsonSerializer.Serialize(response.Items[0]);

        }
        private static string SelectThumbnail(Google.Apis.YouTube.v3.Data.ThumbnailDetails thumbnails)
        {
            if (thumbnails.Maxres != null)
            {
                return thumbnails.Maxres.Url;
            }
            else if (thumbnails.Standard != null)
            {
                return thumbnails.Standard.Url;
            }
            else if (thumbnails.High != null)
            {
                return thumbnails.High.Url;
            }
            else if (thumbnails.Medium != null)
            {
                return thumbnails.Medium.Url;
            }
            else
            {
                return thumbnails.Default__.Url;
            }
        }
        public static async Task APIDownload(string id, IServerApplicationPaths appPaths, CancellationToken cancellationToken)
        {
            //await Task.Delay(10000, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Plugin.Instance.Configuration.ApiKey,
                ApplicationName = "Youtube Metadata"
            });

            var result = new YTDLData();
            if (typeof(D) == typeof(Google.Apis.YouTube.v3.Data.Video))
            {  
                var json = JsonSerializer.Deserialize<Google.Apis.YouTube.v3.Data.Video>(await Download(id, youtubeService.Videos));
                result.uploader = json.Snippet.ChannelTitle;
                if (json.Snippet.PublishedAt.HasValue)
                {
                    result.upload_date = json.Snippet.PublishedAt.Value.ToString("yyyyMMdd");
                }
                result.title = json.Snippet.Title;
                result.description = json.Snippet.Description;
                result.channel_id = json.Snippet.ChannelId;
                result.thumbnail = SelectThumbnail(json.Snippet.Thumbnails);
            }
            else if (typeof(D) == typeof(Google.Apis.YouTube.v3.Data.Channel))
            {
                var json = JsonSerializer.Deserialize<D>(await Download(id, youtubeService.Channels));
            }
            var path = Utils.GetVideoInfoPath(appPaths, id);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonSerializer.Serialize(result));
        }
    }
}
