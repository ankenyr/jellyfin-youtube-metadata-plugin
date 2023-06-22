using System.Collections.Generic;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers.LocalMetadata
{
    public class YoutubeLocalSeriesImageProvider : ILocalImageProvider
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<YoutubeLocalImageProvider> _logger;

        /// <summary>
        /// Providers name, this does not appear in the library metadata settings.
        /// </summary>
        public string Name => Constants.PluginName;

        public YoutubeLocalSeriesImageProvider(IFileSystem fileSystem, ILogger<YoutubeLocalImageProvider> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }
        /// <summary>
        /// Returns boolean based on support of item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Supports(BaseItem item) => item is Series;
        private string GetSeriesInfo(string path)
        {
            _logger.LogDebug("YTLocalImageSeries GetSeriesInfo: {Path}", path);
            Matcher matcher = new();
            matcher.AddInclude("**/*.jpg");
            matcher.AddInclude("**/*.webp");
            Regex rx = new Regex(Constants.YTCHANNEL_RE, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            string infoPath = "";
            foreach (string file in matcher.GetResultsInFullPath(path))
            {
                if (rx.IsMatch(file))
                {
                    infoPath = file;
                    break;
                }
            }
            _logger.LogDebug("YTLocalImageSeries GetSeriesInfo Result: {InfoPath}", infoPath);
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
            _logger.LogDebug("YTLocalImageSeries GetImages: {Name}", item.Name);
            var list = new List<LocalImageInfo>();
            string jpgPath = GetSeriesInfo(item.Path);
            if (String.IsNullOrEmpty(jpgPath))
            {
                return list;
            }
            var localimg = new LocalImageInfo();
            var fileInfo = _fileSystem.GetFileSystemInfo(jpgPath);
            localimg.FileInfo = fileInfo;
            list.Add(localimg);
            _logger.LogDebug("YTLocalImageSeries GetImages Result: {Result}", list.ToString());
            return list;
        }

    }
}
