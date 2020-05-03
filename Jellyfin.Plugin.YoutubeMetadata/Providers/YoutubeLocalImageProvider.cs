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
    public class YoutubeLocalImageProvider : ILocalImageFileProvider
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public YoutubeLocalImageProvider(IServerConfigurationManager config, IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }
        // This does not look neccesary?
        public string Name => "YoutubeMetadata";
        public List<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
        {
            _logger.LogInformation(item.Path);
            var list = new List<LocalImageInfo>();
            var filename = item.FileNameWithoutExtension + ".jpg";
            var fullpath = Path.Combine(item.ContainingFolderPath, filename);
            try
            {
                var localimg = new LocalImageInfo();
                var fileInfo = _fileSystem.GetFileSystemInfo(fullpath);
                localimg.FileInfo = fileInfo;
                list.Add(localimg);
            }
            catch (FileNotFoundException)
            {
                _logger.LogInformation("Could not find {0}", filename);
            }
            return list;
        }
        public bool Supports(BaseItem item)
            => item is Movie;
    }
}
