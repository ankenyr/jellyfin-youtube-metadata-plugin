using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using NYoutubeDL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.YoutubeMetadata
{
    /// <summary>
    /// Represents a search result from YouTube.
    /// </summary>
    public class YTSearchResult
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string ChannelId { get; set; }
        public string Uploader { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    public class Utils
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
                Type = PersonKind.Director,
                ProviderIds = channel_id is null ?
                        new Dictionary<string, string> {} : new Dictionary<string, string> {
                                { Constants.PluginName, channel_id }
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

        public static async Task<string> SearchChannel(string query, IServerApplicationPaths appPaths, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ytd = new YoutubeDLP();
            var url = String.Format(Constants.SearchQuery, System.Web.HttpUtility.UrlEncode(query));
            ytd.Options.VerbositySimulationOptions.Simulate = true;
            ytd.Options.GeneralOptions.FlatPlaylist = true;
            ytd.Options.VideoSelectionOptions.PlaylistItems = "1";
            ytd.Options.VerbositySimulationOptions.Print = "url";
            List<string> ytdl_errs = new();
            List<string> ytdl_out = new();
            ytd.StandardErrorEvent += (sender, error) => ytdl_errs.Add(error);
            ytd.StandardOutputEvent += (sender, output) => ytdl_out.Add(output);
            var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
            if (File.Exists(cookie_file))
            {
                ytd.Options.FilesystemOptions.Cookies = cookie_file;
            }
            var task = ytd.DownloadAsync(url);
            await task;
            if (ytdl_out.Count > 0)
            {
                Uri uri = new Uri(ytdl_out[0]);
                return uri.Segments[uri.Segments.Length - 1];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Searches YouTube for videos matching the query.
        /// </summary>
        /// <param name="query">Search query string.</param>
        /// <param name="appPaths">Application paths for cookie file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="maxResults">Maximum number of results to return.</param>
        /// <returns>List of search results.</returns>
        public static async Task<List<YTSearchResult>> SearchVideos(string query, IServerApplicationPaths appPaths, CancellationToken cancellationToken, int maxResults = 10)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var results = new List<YTSearchResult>();
            var ytd = new YoutubeDLP();
            var url = String.Format(Constants.VideoSearchQuery, System.Web.HttpUtility.UrlEncode(query));
            ytd.Options.VerbositySimulationOptions.Simulate = true;
            ytd.Options.GeneralOptions.FlatPlaylist = true;
            ytd.Options.VideoSelectionOptions.PlaylistItems = $"1:{maxResults}";
            // Print unit-separator-separated: id, title, channel_id, uploader, thumbnail
            ytd.Options.VerbositySimulationOptions.Print = "%(id)s\x1f%(title)s\x1f%(channel_id)s\x1f%(uploader)s\x1f%(thumbnail)s";
            List<string> ytdl_out = new();
            ytd.StandardOutputEvent += (sender, output) => ytdl_out.Add(output);
            var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
            if (File.Exists(cookie_file))
            {
                ytd.Options.FilesystemOptions.Cookies = cookie_file;
            }
            await ytd.DownloadAsync(url);
            foreach (var line in ytdl_out)
            {
                var parts = line.Split('\x1f');
                if (parts.Length >= 5)
                {
                    results.Add(new YTSearchResult
                    {
                        Id = parts[0],
                        Title = parts[1],
                        ChannelId = parts[2],
                        Uploader = parts[3],
                        ThumbnailUrl = parts[4]
                    });
                }
            }
            return results;
        }

        /// <summary>
        /// Searches YouTube for channels matching the query.
        /// </summary>
        /// <param name="query">Search query string.</param>
        /// <param name="appPaths">Application paths for cookie file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="maxResults">Maximum number of results to return.</param>
        /// <returns>List of search results.</returns>
        public static async Task<List<YTSearchResult>> SearchChannels(string query, IServerApplicationPaths appPaths, CancellationToken cancellationToken, int maxResults = 10)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var results = new List<YTSearchResult>();
            var ytd = new YoutubeDLP();
            var url = String.Format(Constants.SearchQuery, System.Web.HttpUtility.UrlEncode(query));
            ytd.Options.VerbositySimulationOptions.Simulate = true;
            ytd.Options.GeneralOptions.FlatPlaylist = true;
            ytd.Options.VideoSelectionOptions.PlaylistItems = $"1:{maxResults}";
            // Print unit-separator-separated: channel_id, uploader (channel name), thumbnail
            ytd.Options.VerbositySimulationOptions.Print = "%(id)s\x1f%(title)s\x1f%(thumbnail)s";
            List<string> ytdl_out = new();
            ytd.StandardOutputEvent += (sender, output) => ytdl_out.Add(output);
            var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
            if (File.Exists(cookie_file))
            {
                ytd.Options.FilesystemOptions.Cookies = cookie_file;
            }
            await ytd.DownloadAsync(url);
            foreach (var line in ytdl_out)
            {
                var parts = line.Split('\x1f');
                if (parts.Length >= 3)
                {
                    results.Add(new YTSearchResult
                    {
                        Id = parts[0],        // Channel ID
                        Title = parts[1],     // Channel name
                        ChannelId = parts[0], // Same as Id for channels
                        Uploader = parts[1],  // Same as Title for channels
                        ThumbnailUrl = parts[2]
                    });
                }
            }
            return results;
        }

        public static async Task<bool> ValidCookie(IServerApplicationPaths appPaths, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ytd = new YoutubeDLP();
            var task = ytd.DownloadAsync("https://www.youtube.com/playlist?list=WL");
            List<string> ytdl_errs = new();
            ytd.StandardErrorEvent += (sender, error) => ytdl_errs.Add(error);
            ytd.Options.VideoSelectionOptions.PlaylistItems = "0";
            ytd.Options.VerbositySimulationOptions.SkipDownload = true;
            var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
            if (File.Exists(cookie_file))
            {
                ytd.Options.FilesystemOptions.Cookies = cookie_file;
            }
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
            var ytd = new YoutubeDLP();
            ytd.Options.VideoSelectionOptions.PlaylistItems = "0";
            ytd.Options.FilesystemOptions.WriteInfoJson = true;
            var dataPath = Path.Combine(appPaths.CachePath, "youtubemetadata", name, "ytvideo");
            ytd.Options.FilesystemOptions.Output = dataPath;
            var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
            if (File.Exists(cookie_file))
            {
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
            var ytd = new YoutubeDLP();
            ytd.Options.FilesystemOptions.WriteInfoJson = true;
            ytd.Options.VerbositySimulationOptions.SkipDownload = true;
            var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
            if (File.Exists(cookie_file))
            {
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
            var date = new DateTime(1970, 1, 1);
            try
            {
                date = DateTime.ParseExact(json.upload_date, "yyyyMMdd", null);
            }
            catch
            {

            }
            result.Item.ProductionYear = date.Year;
            result.Item.PremiereDate = date;
            result.AddPerson(Utils.CreatePerson(json.uploader, json.channel_id));
            result.Item.ProviderIds.Add(Constants.PluginName, json.id);
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
            var date = new DateTime(1970, 1, 1);
            try
            {
                date = DateTime.ParseExact(json.upload_date, "yyyyMMdd", null);
            }
            catch
            {

            }
            result.Item.ProductionYear = date.Year;
            result.Item.PremiereDate = date;
            result.AddPerson(Utils.CreatePerson(json.uploader, json.channel_id));
            result.Item.ProviderIds.Add(Constants.PluginName, json.id);
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
            var date = new DateTime(1970, 1, 1);
            try
            {
                date = DateTime.ParseExact(json.upload_date, "yyyyMMdd", null);
            }
            catch
            {

            }
            result.Item.ProductionYear = date.Year;
            result.Item.PremiereDate = date;
            result.Item.ForcedSortName = date.ToString("yyyyMMdd") + "-" + result.Item.Name;
            result.AddPerson(Utils.CreatePerson(json.uploader, json.channel_id));
            result.Item.IndexNumber = 1;
            result.Item.ParentIndexNumber = 1;
            result.Item.ProviderIds.Add(Constants.PluginName, json.id);
            return result;
        }

        /// <summary>
        /// Provides a Video Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<Video> YTDLJsonToVideo(YTDLData json)
        {
            var item = new Video();
            var result = new MetadataResult<Video>
            {
                HasMetadata = true,
                Item = item
            };
            result.Item.Name = json.title;
            result.Item.Overview = json.description;
            var date = new DateTime(1970, 1, 1);
            try
            {
                date = DateTime.ParseExact(json.upload_date, "yyyyMMdd", null);
            }
            catch
            {

            }
            result.Item.ProductionYear = date.Year;
            result.Item.PremiereDate = date;
            result.AddPerson(Utils.CreatePerson(json.uploader, json.channel_id));
            result.Item.ProviderIds.Add(Constants.PluginName, json.id);
            return result;
        }

        /// <summary>
        /// Provides a Series Metadata Result from a json object.
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
            result.Item.ProviderIds.Add(Constants.PluginName, json.channel_id);
            return result;
        }
    }
}