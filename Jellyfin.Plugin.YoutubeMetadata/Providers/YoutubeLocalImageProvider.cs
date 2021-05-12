using System.IO;
using System.Collections.Generic;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YoutubeLocalImageProvider : ILocalImageProvider, IHasOrder
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<YoutubeLocalImageProvider> _logger;

        public YoutubeLocalImageProvider(IFileSystem fileSystem, ILogger<YoutubeLocalImageProvider> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <summary>
        /// Providers name, this does not appear in the library metadata settings.
        /// </summary>
        public string Name => "YouTube Local Image Metadata";
        public int Order => 1;
        public List<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
        {
            _logger.LogInformation(item.Path);
            var list = new List<LocalImageInfo>();
            if (Plugin.Instance.Configuration.DisableLocalMetadata)
            {
                _logger.LogInformation("Local Metadata Disabled");
                return list;
            }
            
            
            var filename = item.FileNameWithoutExtension + ".jpg";
            var fullpath = Path.Combine(item.ContainingFolderPath, filename);

            var localimg = new LocalImageInfo();
            var fileInfo = _fileSystem.GetFileSystemInfo(fullpath);
            if (File.Exists(fileInfo.FullName))
            {
                localimg.FileInfo = fileInfo;
                list.Add(localimg);
            }
            return list;
        }
        public bool Supports(BaseItem item)
            => item is Movie;
    }
}
