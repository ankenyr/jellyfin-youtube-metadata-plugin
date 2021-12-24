using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{

    /// <summary>
    /// Object should match how YTDL json looks.
    /// </summary>
    public class YTData
    {
#pragma warning disable IDE1006 // Naming Styles
        // Human name
        public string uploader { get; set; }
        public string upload_date { get; set; }
        public string uploader_id { get; set; }
        // https://github.com/ytdl-org/youtube-dl/issues/1806
        public string title { get; set; }
        public string description { get; set; }
        // Name for use in API?
        public string channel_id { get; set; }
        public string track { get; set; }
        public string artist { get; set; }
        public string album { get; set; }
        public string thumbnail { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }
    public abstract class AbstractYoutubeRemoteProvider<B, T, E> : IRemoteMetadataProvider<T, E>
        where T : BaseItem, IHasLookupInfo<E>
        where E : ItemLookupInfo, new()
    {
        public const string YTID_RE = @"(?<=\[)[a-zA-Z0-9\-_]{11}(?=\])";
        protected readonly IServerConfigurationManager _config;
        protected readonly ILogger<B> _logger;
        protected readonly IFileSystem _fileSystem;
        protected readonly System.IO.Abstractions.IFileSystem _afs;

        public AbstractYoutubeRemoteProvider(IFileSystem fileSystem,
            ILogger<B> logger,
            IServerConfigurationManager config,
        System.IO.Abstractions.IFileSystem afs)
        {
            _config = config;
            _fileSystem = fileSystem;
            _logger = logger;
            _afs = afs;
        }
        public abstract string Name { get; }

        /// <summary>
        /// Provides a Movie Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<Movie> YTDLJsonToMovie(YTData json)
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
        public static MetadataResult<Series> YTDLJsonToSeries(YTData json)
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

        /// <summary>
        /// Provides a MusicVideo Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<Season> YTDLJsonToSeason(YTData json)
        {
            var item = new Season();
            var result = new MetadataResult<Season>
            {
                HasMetadata = true,
                Item = item
            };

            result.Item.Name = "";

            return result;
        }

        /// <summary>
        /// Provides a MusicVideo Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<MusicVideo> YTDLJsonToMusicVideo(YTData json)
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
        public static MetadataResult<Episode> YTDLJsonToEpisode(YTData json)
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
        /// Reads JSON data from file.
        /// </summary>
        /// <param name="metaFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public YTData ReadYTDLInfo(string fpath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string jsonString = _afs.File.ReadAllText(fpath);
            var foo = JsonSerializer.Deserialize<YTData>(jsonString);
            return foo;
        }

        public async Task<MetadataResult<T>> GetMetadata(E info, CancellationToken cancellationToken)
        {
            MetadataResult<T> result = new();
            var id = "";
            if (typeof(T) == typeof(Series))
            {
                
                id = info.Name;
            }
            else
            {
                // Episode, Movie, or Music
                id = GetYTID(info.Path);
            }

            
            
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogInformation("Youtube ID not found in filename of title: {info.Name}", info.Name);
                result.HasMetadata = false;
                return result;
            }
            var ytPath = GetVideoInfoPath(this._config.ApplicationPaths, id);
            var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
            if (!IsFresh(fileInfo))
            {
                await this.GetAndCacheMetadata(id, this._config.ApplicationPaths, cancellationToken);
            }
            var video = ReadYTDLInfo(ytPath, cancellationToken);
            if (video != null)
            {
                result = this.GetMetadataImpl(video);
            }
            return result;
        }
        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(E searchInfo, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        internal abstract MetadataResult<T> GetMetadataImpl(YTData jsonObj);

        internal abstract Task GetAndCacheMetadata(string id, IServerApplicationPaths appPaths, CancellationToken cancellationToken);

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
