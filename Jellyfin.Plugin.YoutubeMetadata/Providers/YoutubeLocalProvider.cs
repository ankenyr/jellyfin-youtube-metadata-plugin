using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class MovieJson
    {
        // Human name
        public string uploader { get; set; }
        public string upload_date { get; set; }
        // https://github.com/ytdl-org/youtube-dl/issues/1806
        public string title { get; set; }
        public string description { get; set; }
        public string thumbnail { get; set; }
        // Name for use in API?
        public string channel_id { get; set; }
    }
    public class YoutubeLocalProvider : ILocalMetadataProvider<Movie>, IHasItemChangeMonitor
    {
        private readonly ILogger<YoutubeLocalProvider> _logger;
        private readonly IJsonSerializer _json;
        private readonly IFileSystem _fileSystem;

        public YoutubeLocalProvider(IFileSystem fileSystem, IJsonSerializer json, ILogger<YoutubeLocalProvider> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _json = json;
        }

        public string Name => "YouTube Metadata";

        private FileSystemMetadata GetInfoJson(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);
            var directoryInfo = fileInfo.IsDirectory ? fileInfo : _fileSystem.GetDirectoryInfo(Path.GetDirectoryName(path));
            var directoryPath = directoryInfo.FullName;
            var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(path) + ".info.json");
            var file = _fileSystem.GetFileInfo(specificFile);
            return file;
        }

        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            var infoJson = GetInfoJson(item.Path);
            var result = infoJson.Exists && _fileSystem.GetLastWriteTimeUtc(infoJson) > item.DateLastSaved;
            return result;
        }

        public Task<MetadataResult<Movie>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>();
            if (Plugin.Instance.Configuration.DisableLocalMetadata)
            {
                _logger.LogInformation("Local Metadata Disabled");
                result.HasMetadata = false;
                return Task.FromResult(result);
            }
            try
            {
                var item = new Movie();
                var infoJson = GetInfoJson(info.Path);
                result.HasMetadata = true;
                result.Item = item;
                var jsonObj = ReadJsonData(result, infoJson.FullName, cancellationToken);
                result.Item.Name = jsonObj.title;
                result.Item.Overview = jsonObj.description;
                var date = DateTime.ParseExact(jsonObj.upload_date, "yyyyMMdd", null);
                result.Item.ProductionYear = date.Year;
                result.Item.PremiereDate = date;

                result.AddPerson(Utils.CreatePerson(jsonObj.uploader, jsonObj.channel_id));
                return Task.FromResult(result);
            }
            catch (FileNotFoundException)
            {
                _logger.LogInformation("Could not find {0}", info.Path);
                result.HasMetadata = false;
                return Task.FromResult(result);
            }

        }

        private MovieJson ReadJsonData(MetadataResult<Movie> movieResult, string metaFile, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _json.DeserializeFromFile<MovieJson>(metaFile);
        }
    }
}
