using System.IO;
using System.Collections.Generic;
using MediaBrowser.Controller.Configuration;
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
        private readonly ILogger _logger;

        public YoutubeLocalImageProvider(IServerConfigurationManager config, IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }
        // This does not look neccesary?
        public string Name => "YouTube Metadata";
        public int Order => 1;
        public List<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
        {
            _logger.LogInformation(item.Path);
            var list = new List<LocalImageInfo>();
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
