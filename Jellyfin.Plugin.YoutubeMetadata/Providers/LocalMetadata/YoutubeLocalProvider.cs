using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YoutubeLocalProvider : ILocalMetadataProvider<Movie>, IHasItemChangeMonitor
    {
        private readonly ILogger<YoutubeLocalProvider> _logger;
        private readonly IFileSystem _fileSystem;

        public YoutubeLocalProvider(IFileSystem fileSystem, ILogger<YoutubeLocalProvider> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <summary>
        /// Providers name, this appears in the library metadata settings.
        /// </summary>
        public string Name => "YouTube Local Metadata";

        private FileSystemMetadata GetInfoJson(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);
            var directoryInfo = fileInfo.IsDirectory ? fileInfo : _fileSystem.GetDirectoryInfo(Path.GetDirectoryName(path));
            var directoryPath = directoryInfo.FullName;
            var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(path) + ".info.json");
            var file = _fileSystem.GetFileInfo(specificFile);
            return file;
        }

        /// <summary>
        /// Returns bolean if item has changed since last recorded.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="directoryService"></param>
        /// <returns></returns>
        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            var infoJson = GetInfoJson(item.Path);
            var result = infoJson.Exists && _fileSystem.GetLastWriteTimeUtc(infoJson) < item.DateLastSaved;
            return result;
        }

        /// <summary>
        /// Retrieves metadata of item.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="directoryService"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
                var jsonObj = Utils.ReadYTDLInfo(infoJson.FullName, cancellationToken);
                result = Utils.MovieJsonToMovie(jsonObj);
            }
            catch (FileNotFoundException)
            {
                _logger.LogInformation("Could not find {0}", info.Path);
                result.HasMetadata = false;
                return Task.FromResult(result);
            }
            return Task.FromResult(result);
        }

        
    }
}
