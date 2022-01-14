using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers.LocalMetadata
{
    public class YoutubeLocalSeriesProvider : ILocalMetadataProvider<Series>, IHasItemChangeMonitor
    {
        protected readonly ILogger<YoutubeLocalSeriesProvider> _logger;
        protected readonly IFileSystem _fileSystem;
        public YoutubeLocalSeriesProvider(IFileSystem fileSystem, ILogger<YoutubeLocalSeriesProvider> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }
        public string Name => Constants.PluginName;


        private string GetSeriesInfo(string path)
        {
            Matcher matcher = new();
            matcher.AddInclude("**/*.info.json");
            string infoPath = "";
            foreach (string file in matcher.GetResultsInFullPath(path))
            {
                if (Regex.Match(file, Constants.YTCHANNEL_RE).Success)
                {
                    infoPath = file;
                    break;
                }
            }
            return infoPath;
        }
        public Task<MetadataResult<Series>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            _logger.LogDebug("GetMetadata: {Path}", info.Path);
            MetadataResult<Series> result = new();
            string infoPath = GetSeriesInfo(info.Path);
            if (String.IsNullOrEmpty(infoPath))
            {
                return Task.FromResult(result);
            }
            var infoJson = Utils.ReadYTDLInfo(infoPath, cancellationToken);
            result = Utils.YTDLJsonToSeries(infoJson);
            return Task.FromResult(result);
        }
        FileSystemMetadata GetInfoJson(string path)
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
            var infoPath = GetSeriesInfo(item.Path);
            if (!String.IsNullOrEmpty(infoPath))
            {
                var infoJson = GetInfoJson(infoPath);
                var result = infoJson.Exists && _fileSystem.GetLastWriteTimeUtc(infoJson) < item.DateLastSaved;
                return result;
            }
            return false;
            
        }
    }
}