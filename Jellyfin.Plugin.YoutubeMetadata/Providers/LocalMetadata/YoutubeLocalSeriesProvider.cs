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
            _logger.LogDebug("YTLocalSeries GetSeriesInfo: {Path}", path);
            Matcher matcher = new();
            matcher.AddInclude("**/*.info.json");
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
            _logger.LogDebug("YTLocalSeries GetSeriesInfo Result: {InfoPath}", infoPath);
            return infoPath;
        }
        public Task<MetadataResult<Series>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            _logger.LogDebug("YTLocalSeries GetMetadata: {Path}", info.Path);
            MetadataResult<Series> result = new();
            string infoPath = GetSeriesInfo(info.Path);
            if (String.IsNullOrEmpty(infoPath))
            {
                return Task.FromResult(result);
            }
            var infoJson = Utils.ReadYTDLInfo(infoPath, cancellationToken);
            result = Utils.YTDLJsonToSeries(infoJson);
            _logger.LogDebug("YTLocalSeries GetMetadata Result: {Result}", result);
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
            _logger.LogDebug("YTLocalSeries HasChanged: {Path}", item.Path);
            var infoPath = GetSeriesInfo(item.Path);
            var result = false;
            if (!String.IsNullOrEmpty(infoPath))
            {
                var infoJson = GetInfoJson(infoPath);
                result = infoJson.Exists && _fileSystem.GetLastWriteTimeUtc(infoJson) < item.DateLastSaved;
            }
            _logger.LogDebug("YTLocalSeries HasChanged Result: {Result}", result);
            return result;

        }
    }
}