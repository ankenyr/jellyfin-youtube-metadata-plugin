﻿using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
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
            var match = Regex.Match(name, Constants.YTID_RE);
            if (!match.Success)
            {
                match = Regex.Match(name, Constants.YTCHANNEL_RE);
            }
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

        public static async Task<string> SearchChannel (string query, IServerApplicationPaths appPaths, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ytd = new YoutubeDL();
            var url = String.Format(Constants.SearchQuery, System.Web.HttpUtility.UrlEncode(query));
            ytd.Options.VerbositySimulationOptions.Simulate = true;
            ytd.Options.GeneralOptions.FlatPlaylist = true;
            ytd.Options.VideoSelectionOptions.PlaylistItems = "1";
            ytd.Options.VerbositySimulationOptions.PrintField = "url";
            List<string> ytdl_errs = new();
            List<string> ytdl_out = new();
            ytd.StandardErrorEvent += (sender, error) => ytdl_errs.Add(error);
            ytd.StandardOutputEvent += (sender, output) => ytdl_out.Add(output);
            var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
            if (!File.Exists(cookie_file))
            {
                return "";
            }
            ytd.Options.FilesystemOptions.Cookies = cookie_file;
            var task = ytd.DownloadAsync(url);
            await task;
            return Utils.GetYTID(ytdl_out[0]);
        }
        public static async Task<bool> ValidCookie(IServerApplicationPaths appPaths, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ytd = new YoutubeDL();
            var task = ytd.DownloadAsync("https://www.youtube.com/playlist?list=WL");
            List<string> ytdl_errs = new();
            ytd.StandardErrorEvent += (sender, error) => ytdl_errs.Add(error);
            ytd.Options.VideoSelectionOptions.PlaylistItems = "0";
            ytd.Options.VerbositySimulationOptions.SkipDownload = true;
            var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
            if (!File.Exists(cookie_file))
            {
                return false;
            }
            ytd.Options.FilesystemOptions.Cookies = cookie_file;
            await task;
            
            foreach (string err in ytdl_errs)
            {
                var match = Regex.Match(err, @".*The playlist does not exist\..*");
                if (match.Success)
                {
                    return false;
                }
            }
            return true;
        }
        public static async Task GetChannelInfo(string id, string name, IServerApplicationPaths appPaths, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ytd = new YoutubeDL();
            ytd.Options.VideoSelectionOptions.PlaylistItems = "0";
            ytd.Options.FilesystemOptions.WriteInfoJson = true;
            var dataPath = Path.Combine(appPaths.CachePath, "youtubemetadata", name, "ytvideo");
            ytd.Options.FilesystemOptions.Output = dataPath;
            var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
            if (File.Exists(cookie_file))
            {
                Console.WriteLine("cookie found");
                ytd.Options.FilesystemOptions.Cookies = cookie_file;
            }
            List<string> ytdl_errs = new();
            ytd.StandardErrorEvent += (sender, error) => ytdl_errs.Add(error);
            var task = ytd.DownloadAsync(String.Format(Constants.ChannelUrl, id));
            await task;
        }
        public static async Task YTDLMetadata(string id, IServerApplicationPaths appPaths, CancellationToken cancellationToken)
        {
            //var foo = await ValidCookie(appPaths, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var ytd = new YoutubeDL();
            ytd.Options.FilesystemOptions.WriteInfoJson = true;
            ytd.Options.VerbositySimulationOptions.SkipDownload = true;
            var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
            if ( File.Exists(cookie_file) ) {
                Console.WriteLine("cookie found");
                ytd.Options.FilesystemOptions.Cookies = cookie_file;
            }
            
            var dlstring = "https://www.youtube.com/watch?v=" + id;
            var dataPath = Path.Combine(appPaths.CachePath, "youtubemetadata", id, "ytvideo");
            ytd.Options.FilesystemOptions.Output = dataPath;
            
            List<string> ytdl_errs = new();
            ytd.StandardErrorEvent += (sender, error) => ytdl_errs.Add(error);
            var task = ytd.DownloadAsync(dlstring);
            await task;
        }
        /// <summary>
        /// Reads JSON data from file.
        /// </summary>
        /// <param name="metaFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static YTDLData ReadYTDLInfo(string fpath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string jsonString = File.ReadAllText(fpath);
            return JsonSerializer.Deserialize<YTDLData>(jsonString);
        }

        /// <summary>
        /// Provides a Movie Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<Movie> YTDLJsonToMovie(YTDLData json)
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
        public static MetadataResult<MusicVideo> YTDLJsonToMusicVideo(YTDLData json)
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
        public static MetadataResult<Episode> YTDLJsonToEpisode(YTDLData json)
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
        /// <summary>
        /// Provides a MusicVideo Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<Series> YTDLJsonToSeries(YTDLData json)
        {
            var item = new Series();
            var result = new MetadataResult<Series>
            {
                HasMetadata = true,
                Item = item
            };

            result.Item.Name = json.uploader;
            result.Item.Overview = json.description;
            return result;
        }
    }

}
