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
    public class YoutubeLocalSeasonProvider : ILocalMetadataProvider<Season>, IHasItemChangeMonitor
    {
        public string Name => Constants.PluginName;

        public Task<MetadataResult<Season>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            _logger.LogDebug("GetMetadata: {Path}", info.Path);
            MetadataResult<Season> result = new();
            var item = new Season();
            item.IndexNumber = 1;
            item.Name = "Season 1";
            item.OriginalTitle = "Season 1";
            result.Item = item;
            result.HasMetadata = true;
            return Task.FromResult(result);
        }

        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            return true;
        }
    }
}
