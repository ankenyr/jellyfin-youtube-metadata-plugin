using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers;

/// <summary>
/// Represents a local metadata provider for YouTube series.
/// </summary>
public class YoutubeLocalSeriesProvider(ILogger<YoutubeLocalSeriesProvider> logger) : ILocalMetadataProvider<Series>
{
    /// <summary>
    /// Gets the name of the plugin.
    /// </summary>
    public string Name => PluginConstants.PluginName;

    /// <summary>
    /// Retrieves the metadata for a series from a local YouTube video file.
    /// </summary>
    /// <param name="info">The information about the video file.</param>
    /// <param name="directoryService">The directory service used to access the file system.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata for the series.</returns>
    public Task<MetadataResult<Series>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken) => GetMetadata(info, directoryService, InfoJsonStreamFactory, cancellationToken);

    /// <summary>
    /// Retrieves the metadata for a series from a local YouTube video file.
    /// </summary>
    /// <param name="info">The information about the video file.</param>
    /// <param name="directoryService">The directory service used to access the file system.</param>
    /// <param name="infoJsonStreamProvider">The function that provides a stream for the info.json file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata for the series.</returns>
    public async Task<MetadataResult<Series>> GetMetadata(ItemInfo info, IDirectoryService directoryService, Func<FileSystemMetadata, Stream> infoJsonStreamProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(info, nameof(info));
        ArgumentNullException.ThrowIfNull(directoryService, nameof(directoryService));

        var infoJsonFile = directoryService.GetFile(Path.ChangeExtension(info.Path, "info.json")) ?? throw new FileNotFoundException("info.json file not found", info.Path);
        using var infoJson = infoJsonStreamProvider(infoJsonFile);

        var infoJsonDto = await JsonSerializer.DeserializeAsync<YTChannelDto>(infoJson, JsonSerializerOptions.Default, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw new JsonException("Failed to deserialize info.json");

        logger.LogInformation("Metadata retrieved for series: {SeriesId}", infoJsonDto.Id);

        return MetadataResultSeriesFactory(infoJsonDto);
    }

    private static FileStream InfoJsonStreamFactory(FileSystemMetadata infoJsonFile) => File.OpenRead(infoJsonFile.FullName);

    private static MetadataResult<Series> MetadataResultSeriesFactory(YTChannelDto infoJsonDto) => new()
    {
        HasMetadata = true,
        People = [
                new()
                {
                    Name = infoJsonDto.Uploader,
                    Type = PersonKind.Creator,
                    ProviderIds = new() { { PluginConstants.PluginName, infoJsonDto.ChannelId } },
                }
            ],
        Item = new()
        {
            Name = infoJsonDto.Uploader,
            Overview = infoJsonDto.Description,
            ProviderIds = new() { { PluginConstants.PluginName, infoJsonDto.Id } },
        },
    };
}
