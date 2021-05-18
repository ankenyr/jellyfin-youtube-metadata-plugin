using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using NYoutubeDL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YoutubeMetadata
{
    class Utils
    {
        public const string YTID_RE = @"(?<=\[)[a-zA-Z0-9\-_]{11}(?=\])";
        public enum DownloadType
        {
            Channel,
            Video
        }
        public class YTDLMovieJson
        {
            // Human name
            public string Uploader { get; set; }
            public string Upload_Date { get; set; }
            // https://github.com/ytdl-org/youtube-dl/issues/1806
            public string Title { get; set; }
            public string Description { get; set; }
            public string Thumbnail { get; set; }
            // Name for use in API?
            public string Channel_Id { get; set; }
        }

        public static bool IsFresh(MediaBrowser.Model.IO.FileSystemMetadata fileInfo)
        {
            if (fileInfo.Exists && DateTime.UtcNow.Subtract(fileInfo.LastWriteTimeUtc).Days <= 10)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///  Returns the Youtube ID from the file path. Matches last 11 character field inside square brackets.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetYTID(string name)
        {
            var match = Regex.Match(name, YTID_RE);
            return match.Value;
        }

        /// <summary>
        /// Creates a person object of type director for the provided name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="channel_id"></param>
        /// <returns></returns>
        public static PersonInfo CreatePerson(string name, string channel_id)
        {
            return new PersonInfo
            {
                Name = name,
                Type = PersonType.Director,
                ProviderIds = new Dictionary<string, string> { { "youtubemetadata", channel_id }
            },
            };
        }

        /// <summary>
        /// Returns path to where metadata json file should be.
        /// </summary>
        /// <param name="appPaths"></param>
        /// <param name="youtubeID"></param>
        /// <returns></returns>
        public static string GetVideoInfoPath(IServerApplicationPaths appPaths, string youtubeID)
        {
            var dataPath = Path.Combine(appPaths.CachePath, "youtubemetadata", youtubeID);
            return Path.Combine(dataPath, "ytvideo.info.json");
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
        public static async Task<string> Download(string id, ChannelsResource resource)
        {
            var vreq = resource.List("snippet");
            vreq.Id = id;
            var response = await vreq.ExecuteAsync();
            return JsonSerializer.Serialize(response.Items[0]);

        }

        /// <summary>
        /// Downloads and stores metadata from the YT API.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="appPaths"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="dtype"></param>
        /// <returns></returns>
        public static async Task APIDownload(string id, IServerApplicationPaths appPaths, DownloadType dtype, CancellationToken cancellationToken)
        {
            await Task.Delay(10000, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Plugin.Instance.Configuration.ApiKey,
                ApplicationName = "Youtube Metadata"
            });
            // Lets change this
            string json = "";
            if (dtype == Utils.DownloadType.Video)
            {
                json = await Download(id, youtubeService.Videos);
            }
            else if (dtype == Utils.DownloadType.Channel)
            {
                json = await Download(id, youtubeService.Channels);
            }
            var path = Utils.GetVideoInfoPath(appPaths, id);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
        }
        public static async Task YTDLMetadata(string id, IServerApplicationPaths appPaths, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ytd = new YoutubeDL();
            ytd.Options.FilesystemOptions.WriteInfoJson = true;
            ytd.Options.VerbositySimulationOptions.SkipDownload = true;
            // Pulled from above, might want to abstract
            var dataPath = Path.Combine(appPaths.CachePath, "youtubemetadata", id, "ytvideo");
            ytd.Options.FilesystemOptions.Output = dataPath;
            var dlstring = "https://www.youtube.com/watch?v=" + id;
            await ytd.DownloadAsync(dlstring);
        }
        /// <summary>
        /// Reads JSON data from file.
        /// </summary>
        /// <param name="metaFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static YTDLMovieJson ReadYTDLInfo(string fpath, IJsonSerializer json, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return json.DeserializeFromFile<YTDLMovieJson>(fpath);
        }

        /// <summary>
        /// Provides a Movie Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<Movie>  MovieJsonToMovie(YTDLMovieJson json)
        {
            var item = new Movie();
            var result = new MetadataResult<Movie>
            {
                HasMetadata = true,
                Item = item
            };
            result.Item.Name = json.Title;
            result.Item.Overview = json.Description;
            var date = DateTime.ParseExact(json.Upload_Date, "yyyyMMdd", null);
            result.Item.ProductionYear = date.Year;
            result.Item.PremiereDate = date;
            result.AddPerson(Utils.CreatePerson(json.Uploader, json.Channel_Id));
            return result;
        }
    }

}