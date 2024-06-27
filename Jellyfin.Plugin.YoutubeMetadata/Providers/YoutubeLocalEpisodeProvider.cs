// <copyright file="YoutubeLocalEpisodeProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Jellyfin.Plugin.YoutubeMetadata.Providers;

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

/// <summary>
/// Provides local metadata for episodes from YouTube videos.
/// </summary>
public class YoutubeLocalEpisodeProvider(ILogger<YoutubeLocalEpisodeProvider> logger) : ILocalMetadataProvider<Episode>
{
    /// <summary>
    /// Gets the name of the plugin.
    /// </summary>
    public string Name => PluginConstants.PluginName;

    /// <summary>
    /// Retrieves the metadata for an episode.
    /// </summary>
    /// <param name="info">The item info.</param>
    /// <param name="directoryService">The directory service.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata result.</returns>
    public Task<MetadataResult<Episode>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken) => GetMetadata(info, directoryService, InfoJsonStreamProvider, cancellationToken);

    /// <summary>
    /// Retrieves the metadata for an episode.
    /// </summary>
    /// <param name="info">The item info.</param>
    /// <param name="directoryService">The directory service.</param>
    /// <param name="infoJsonStreamFactory">The info.json stream factory.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata result.</returns>
    public async Task<MetadataResult<Episode>> GetMetadata(ItemInfo info, IDirectoryService directoryService, Func<FileSystemMetadata, Stream> infoJsonStreamFactory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(info, nameof(info));
        ArgumentNullException.ThrowIfNull(directoryService, nameof(directoryService));

        var infoJsonFile = directoryService.GetFile(Path.ChangeExtension(info.Path, "info.json")) ?? throw new FileNotFoundException("info.json file not found", info.Path);
        using var infoJson = infoJsonStreamFactory(infoJsonFile);

        var infoJsonDto = await JsonSerializer.DeserializeAsync<YTVideoDto>(infoJson, JsonSerializerOptions.Default, cancellationToken).ConfigureAwait(false) ?? throw new JsonException("Failed to deserialize info.json");

        var episode = MetadataResultEpisodeFactory(infoJsonDto);

        logger.LogInformation("Metadata retrieved for episode: {EpisodeId}", infoJsonDto.Id);

        return episode;
    }

    private static FileStream InfoJsonStreamProvider(FileSystemMetadata infoJsonFile) => File.OpenRead(infoJsonFile.FullName);

    private static MetadataResult<Episode> MetadataResultEpisodeFactory(YTVideoDto infoJsonDto) => new()
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
            Name = infoJsonDto.Title,
            Overview = infoJsonDto.Description,
            PremiereDate = DateTime.ParseExact(infoJsonDto.UploadDate, "yyyyMMdd", null),
            IndexNumber = infoJsonDto.PlaylistIndex,
            ParentIndexNumber = 1,
            ProviderIds = new() { { PluginConstants.PluginName, infoJsonDto.Id } },
        },
    };
}
