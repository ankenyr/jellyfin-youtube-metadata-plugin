using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    /// <summary>
    /// Tvdb series provider.
    /// </summary>
    public class YTDLSeriesProvider : AbstractYoutubeRemoteProvider<YTDLSeriesProvider, Series, SeriesInfo>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="TvdbSeriesProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{TvdbSeriesProvider}"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public YTDLSeriesProvider(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            ILogger<YTDLSeriesProvider> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, httpClientFactory, logger, config, afs)
        {
        }

        /// <inheritdoc />
        public override string Name => Constants.PluginName;

        public override async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            _logger.LogDebug("YTDLSeries GetMetadata: {Path}", info.Path);
            MetadataResult<Series> result = new();
            var name = info.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogDebug("YTDLSeries GetMetadata: No name found for media: {Path}", info.Path);
                result.HasMetadata = false;
                return result;
            }
            var ytPath = GetVideoInfoPath(this._config.ApplicationPaths, name);
            var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
            _logger.LogDebug("YTDLSeries GetMetadata: FileInfo: {Path} ", fileInfo.Name);
            if (!IsFresh(fileInfo))
            {
                _logger.LogDebug("YTDLSeries GetMetadata: {Name} is not fresh.", fileInfo.Name);
                await this.GetAndCacheMetadata(name, this._config.ApplicationPaths, cancellationToken);
            }
            var video = ReadYTDLInfo(ytPath, cancellationToken);
            if (video != null)
            {
                try
                {
                    result = this.GetMetadataImpl(video, video.channel_id);
                }
                catch (System.ArgumentException e)
                {
                    _logger.LogError(e,
                        "YTDLSeries GetMetadata: Error parsing json: {Video} {Title}",
                        video.ToString(),
                        video.title);
                }
            }
            return result;
        }

        internal override MetadataResult<Series> GetMetadataImpl(YTDLData jsonObj, string id) => Utils.YTDLJsonToSeries(jsonObj);

        internal async override Task GetAndCacheMetadata(
            string name,
            IServerApplicationPaths appPaths,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("YTDLSeries GetMetadataImpl: GetAndCacheMetadata {Name}", name);
            var ytPath = GetVideoInfoPath(this._config.ApplicationPaths, name);
            var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
            if (!IsFresh(fileInfo))
            {
                _logger.LogDebug("YTDLSeries GetMetadataImpl: {Name} is not fresh", fileInfo.Name);
                var searchResult = Utils.SearchChannel(name, appPaths, cancellationToken);
                await searchResult;
                await Utils.GetChannelInfo(searchResult.Result, name, appPaths, cancellationToken);
            }

        }

        /// <summary>
        /// Searches for YouTube channels matching the query.
        /// </summary>
        public override async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            _logger.LogDebug("YTDLSeries GetSearchResults: Name={Name}", searchInfo.Name);
            var results = new List<RemoteSearchResult>();

            // Check if we have a direct YouTube channel ID to look up
            if (searchInfo.ProviderIds.TryGetValue(Constants.PluginName, out var channelId) && !string.IsNullOrEmpty(channelId))
            {
                _logger.LogDebug("YTDLSeries GetSearchResults: Looking up by ChannelID={ID}", channelId);
                // Fetch metadata for this specific channel
                var tempName = $"_search_{channelId}";
                try
                {
                    await Utils.GetChannelInfo(channelId, tempName, this._config.ApplicationPaths, cancellationToken);
                    var ytPath = GetVideoInfoPath(this._config.ApplicationPaths, tempName);
                    var video = ReadYTDLInfo(ytPath, cancellationToken);
                    if (video != null)
                    {
                        var result = new RemoteSearchResult
                        {
                            Name = video.uploader,
                            SearchProviderName = Name,
                            ImageUrl = video.thumbnails?.Count > 0 ? video.thumbnails[^1].url : null,
                            Overview = video.description?.Length > 200 ? video.description.Substring(0, 200) + "..." : video.description,
                        };
                        result.ProviderIds = new Dictionary<string, string> { { Constants.PluginName, video.channel_id } };
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "YTDLSeries GetSearchResults: Failed to get metadata for ChannelID={ID}", channelId);
                }
                return results;
            }

            // Search by name
            var searchQuery = searchInfo.Name;
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return results;
            }

            _logger.LogDebug("YTDLSeries GetSearchResults: Searching for '{Query}'", searchQuery);
            var searchResults = await Utils.SearchChannels(searchQuery, this._config.ApplicationPaths, cancellationToken);

            foreach (var item in searchResults)
            {
                var result = new RemoteSearchResult
                {
                    Name = item.Title,
                    SearchProviderName = Name,
                    ImageUrl = item.ThumbnailUrl,
                };
                result.ProviderIds = new Dictionary<string, string> { { Constants.PluginName, item.Id } };
                results.Add(result);
            }

            return results;
        }

        public override Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            _logger.LogDebug("YTDLSeries GetImageResponse: {URL}", url);
            return _httpClientFactory.CreateClient(Constants.PluginName).GetAsync(url, cancellationToken);
        }
    }
}
