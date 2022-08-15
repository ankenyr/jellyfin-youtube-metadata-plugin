using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public abstract class AbstractYoutubeLocalProvider<B, T> : ILocalMetadataProvider<T>, IHasItemChangeMonitor where T : BaseItem
    {
        protected readonly ILogger<B> _logger;
        protected readonly IFileSystem _fileSystem;

        /// <summary>
        /// Providers name, this appears in the library metadata settings.
        /// </summary>
        public abstract string Name { get; }

        public AbstractYoutubeLocalProvider(IFileSystem fileSystem, ILogger<B> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        protected FileSystemMetadata GetInfoJson(string path)
        {
            _logger.LogDebug("YTLocal GetInfoJson: {Path}", path);
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
            _logger.LogDebug("YTLocal HasChanged: {Name}", item.Name);
            var infoJson = GetInfoJson(item.Path);
            var result = infoJson.Exists && _fileSystem.GetLastWriteTimeUtc(infoJson) < item.DateLastSaved;
            _logger.LogDebug("YTLocal HasChanged Result: {Result}", result);
            return result;
        }

        /// <summary>
        /// Retrieves metadata of item.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="directoryService"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<MetadataResult<T>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            _logger.LogDebug("YTLocal GetMetadata: {Path}", info.Path);
            var result = new MetadataResult<T>();
            var infoFile = Path.ChangeExtension(info.Path, "info.json");
            if (!File.Exists(infoFile))
            {
                return Task.FromResult(result);
            }
            var jsonObj = Utils.ReadYTDLInfo(infoFile, cancellationToken);
            _logger.LogDebug("YTLocal GetMetadata Result: {JSON}", jsonObj.ToString());
            result = this.GetMetadataImpl(jsonObj);

            return Task.FromResult(result);
        }

        internal abstract MetadataResult<T> GetMetadataImpl(YTDLData jsonObj);
    }
}
