using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers.LocalMetadata
{
    public class YoutubeLocalSeasonProvider : ILocalMetadataProvider<Season>, IHasItemChangeMonitor
    {
        protected readonly ILogger<YoutubeLocalSeasonProvider> _logger;
        public string Name => Constants.PluginName;
        public YoutubeLocalSeasonProvider(ILogger<YoutubeLocalSeasonProvider> logger)
        {
            _logger = logger;
        }
        public Task<MetadataResult<Season>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            _logger.LogDebug("GetMetadata: {Path}", info.Path);
            MetadataResult<Season> result = new();
            var item = new Season();
            item.Name = Path.GetFileNameWithoutExtension(info.Path);
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
