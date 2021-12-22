﻿using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Newtonsoft.Json.Linq;
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
        public class ThumbnailInfo
        {
            public string URL { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public string Resolution { get; set; }
            public string Id { get; set; }
        }
        public class YTDLMovieJson
        {
            // Human name
#pragma warning disable IDE1006 // Naming Styles
            public string uploader { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
            public string upload_date { get; set; }
#pragma warning restore IDE1006 // Naming Styles
            // https://github.com/ytdl-org/youtube-dl/issues/1806
#pragma warning disable IDE1006 // Naming Styles
            public string title { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
            public string description { get; set; }
#pragma warning restore IDE1006 // Naming Styles
            // Name for use in API?
#pragma warning disable IDE1006 // Naming Styles
            public string channel_id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
            public string track { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
            public string artist { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
            public string album { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
            public List<ThumbnailInfo> thumbnails { get; set; }
#pragma warning restore IDE1006 // Naming Styles

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
        


        public static async Task YTDLMetadata(string id, IServerApplicationPaths appPaths, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ytd = new YoutubeDL();
            ytd.Options.FilesystemOptions.WriteInfoJson = true;
            ytd.Options.VerbositySimulationOptions.SkipDownload = true;
            Console.WriteLine("inside YTDLMetadata");
            var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookie.txt");
            if ( File.Exists(cookie_file) ) {
                Console.WriteLine("cookie found");
                ytd.Options.FilesystemOptions.Cookies = cookie_file;
            }
            // Pulled from above, might want to abstract
            var dataPath = Path.Combine(appPaths.CachePath, "youtubemetadata", id, "ytvideo");
            ytd.Options.FilesystemOptions.Output = dataPath;
            var dlstring = "https://www.youtube.com/watch?v=" + id;
            List<string> ytdl_errs = new();
            ytd.StandardErrorEvent += (sender, error) => ytdl_errs.Add(error);
            var task = ytd.DownloadAsync(dlstring);
            if (await Task.WhenAny(task, Task.Delay(10000, cancellationToken)) == task)
            {
                await task;
            }
            else
            {
                throw new Exception(String.Format("Timeout error for video id: {0}, errors: {1}", id, String.Join(" ", ytdl_errs)));
            }

        }
        /// <summary>
        /// Reads JSON data from file.
        /// </summary>
        /// <param name="metaFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static YTDLMovieJson ReadYTDLInfo(string fpath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string jsonString = File.ReadAllText(fpath);
            return JsonSerializer.Deserialize<YTDLMovieJson>(jsonString);
        }

        /// <summary>
        /// Provides a Movie Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<Movie> YTDLJsonToMovie(YTDLMovieJson json)
        {
            var item = new Movie();
            var result = new MetadataResult<Movie>
            {
                HasMetadata = true,
                Item = item
            };
            result.Item.Name = json.title;
            result.Item.Overview = json.description;
            var date = DateTime.ParseExact(json.upload_date, "yyyyMMdd", null);
            result.Item.ProductionYear = date.Year;
            result.Item.PremiereDate = date;
            result.AddPerson(Utils.CreatePerson(json.uploader, json.channel_id));
            return result;
        }

        /// <summary>
        /// Provides a MusicVideo Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<MusicVideo> YTDLJsonToMusicVideo(YTDLMovieJson json)
        {
            var item = new MusicVideo();
            var result = new MetadataResult<MusicVideo>
            {
                HasMetadata = true,
                Item = item
            };

            result.Item.Name = String.IsNullOrEmpty(json.track) ? json.title : json.track;
            result.Item.Artists = new List<string> { json.artist };
            result.Item.Album = json.album;
            result.Item.Overview = json.description;

            var date = DateTime.ParseExact(json.upload_date, "yyyyMMdd", null);
            result.Item.ProductionYear = date.Year;
            result.Item.PremiereDate = date;

            result.AddPerson(Utils.CreatePerson(json.uploader, json.channel_id));

            return result;
        }

        /// <summary>
        /// Provides a Episode Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<Episode> YTDLJsonToEpisode(YTDLMovieJson json)
        {
            var item = new Episode();
            var result = new MetadataResult<Episode>
            {
                HasMetadata = true,
                Item = item
            };

            result.Item.Name = json.title;
            result.Item.Overview = json.description;
            var date = DateTime.ParseExact(json.upload_date, "yyyyMMdd", null);
            result.Item.ProductionYear = date.Year;
            result.Item.PremiereDate = date;
            result.Item.ForcedSortName = date.ToString("yyyyMMdd") + "-" + result.Item.Name;
            result.AddPerson(Utils.CreatePerson(json.uploader, json.channel_id));
            result.Item.IndexNumber = 1;
            result.Item.ParentIndexNumber = 1;
            return result;
        }
    }

}
