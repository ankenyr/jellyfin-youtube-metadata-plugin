using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers;

/// <summary>
/// Provides local metadata for a season using YouTube as the data source.
/// </summary>
public class YoutubeLocalSeasonProvider() : ILocalMetadataProvider<Season>
{
    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    public string Name => PluginConstants.PluginName;

    /// <summary>
    /// Retrieves the metadata for a season based on the provided item information.
    /// </summary>
    /// <param name="info">The item information.</param>
    /// <param name="directoryService">The directory service.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata result for the season.</returns>
    public Task<MetadataResult<Season>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken) => Task.FromResult(new MetadataResult<Season>()
    {
        Item = new Season
        {
            Name = Path.GetFileNameWithoutExtension(info.Path),
        },
        HasMetadata = true,
    });
}
