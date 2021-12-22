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

        /// <summary>
        /// Retrieves Image.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="directoryService"></param>
        /// <returns></returns>
        public IEnumerable<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
        {
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

        /// <summary>
        /// Returns boolean based on support of item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Supports(BaseItem item)
            => item is Movie;
    }
}
