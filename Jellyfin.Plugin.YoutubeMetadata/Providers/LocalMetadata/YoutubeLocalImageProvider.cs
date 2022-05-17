using System.Collections.Generic;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using MediaBrowser.Controller.Entities.TV;

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
        public string Name => Constants.PluginName;

        public int Order => 1;
        private string GetSeriesInfo(string path)
        {
            _logger.LogDebug("YTLocalImage GetSeriesInfo: {Path}", path);
            Matcher matcher = new();
            matcher.AddInclude("*.jpg");
            matcher.AddInclude("*.webp");
            string infoPath = "";
            foreach (string file in matcher.GetResultsInFullPath(path))
            {
                if (Regex.Match(file, Constants.YTID_RE).Success)
                {
                    infoPath = file;
                    break;
                }
            }
            _logger.LogDebug("YTLocalImage GetSeriesInfo Result: {InfoPath}", infoPath);
            return infoPath;
        }
        /// <summary>
        /// Retrieves Image.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="directoryService"></param>
        /// <returns></returns>
        public IEnumerable<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
        {
            _logger.LogDebug("YTLocalImage GetImages: {Name}", item.Name);
            var list = new List<LocalImageInfo>();
            string jpgPath = GetSeriesInfo(item.ContainingFolderPath);
            if (String.IsNullOrEmpty(jpgPath))
            {
                return list;
            }
            if (String.IsNullOrEmpty(jpgPath))
            {
                return list;
            }
            var localimg = new LocalImageInfo();
            var fileInfo = _fileSystem.GetFileSystemInfo(jpgPath);
            localimg.FileInfo = fileInfo;
            list.Add(localimg);
            return list;
        }

        /// <summary>
        /// Returns boolean based on support of item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Supports(BaseItem item)
            => item is Movie || item is Episode || item is MusicVideo;
    }
}
