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
    public abstract class AbstractYoutubeRemoteProvider<B, T, E> : IRemoteMetadataProvider<T, E>
        where T : BaseItem, IHasLookupInfo<E>
        where E : ItemLookupInfo, new()
    {
        protected readonly IServerConfigurationManager _config;
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly ILogger<B> _logger;
        protected readonly IFileSystem _fileSystem;
        protected readonly System.IO.Abstractions.IFileSystem _afs;

        public AbstractYoutubeRemoteProvider(IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            ILogger<B> logger,
            IServerConfigurationManager config,
        System.IO.Abstractions.IFileSystem afs)
        {
            _config = config;
            _fileSystem = fileSystem;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _afs = afs;
        }
        public abstract string Name { get; }

        /// <summary>
        /// Provides a Movie Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<Movie> YTDLJsonToMovie(YTDLData json, string id)
        {
            var result = Utils.YTDLJsonToMovie(json);
            return result;
        }

        /// <summary>
        /// Provides a MusicVideo Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<MusicVideo> YTDLJsonToMusicVideo(YTDLData json, string id)
        {
            var result = Utils.YTDLJsonToMusicVideo(json);
            return result;
        }

        /// <summary>
        /// Provides a Episode Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<Episode> YTDLJsonToEpisode(YTDLData json, string id)
        {
            var result = Utils.YTDLJsonToEpisode(json);
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
            var match = Regex.Match(name, Constants.YTID_RE);
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
        public YTDLData ReadYTDLInfo(string fpath, CancellationToken cancellationToken)
        {
            _logger.LogDebug("YTDL ReadYTDLInfo: {Path}", fpath);
            cancellationToken.ThrowIfCancellationRequested();
            string jsonString = _afs.File.ReadAllText(fpath);
            var json = JsonSerializer.Deserialize<YTDLData>(jsonString);
            return json;
        }

        public virtual async Task<MetadataResult<T>> GetMetadata(E info, CancellationToken cancellationToken)
        {
            _logger.LogDebug("YTDL GetMetadata: {Path}", info.Path);
            MetadataResult<T> result = new();
            var id = GetYTID(info.Path);
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogInformation("YTDL GetMetadata: Youtube ID not found in filename of title: {info.Name}", info.Name);
                result.HasMetadata = false;
                return result;
            }
            var ytPath = GetVideoInfoPath(this._config.ApplicationPaths, id);
            var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
            if (!IsFresh(fileInfo))
            {
                _logger.LogDebug("YTDL GetMetadata: Not Fresh: {ID}", id);
                await this.GetAndCacheMetadata(id, this._config.ApplicationPaths, cancellationToken);
            }
            var video = ReadYTDLInfo(ytPath, cancellationToken);
            if (video != null)
            {
                _logger.LogDebug("YTDL GetMetadata: Calling Impl function: {ID}", id);
                result = this.GetMetadataImpl(video, id);
            }
            return result;
        }
        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(E searchInfo, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        internal abstract MetadataResult<T> GetMetadataImpl(YTDLData jsonObj, string id);

        internal abstract Task GetAndCacheMetadata(string id, IServerApplicationPaths appPaths, CancellationToken cancellationToken);

        public virtual Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
