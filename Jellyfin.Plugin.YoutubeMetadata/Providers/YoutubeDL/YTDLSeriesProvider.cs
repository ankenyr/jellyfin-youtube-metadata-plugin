using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    /// <summary>
    /// Tvdb series provider.
    /// </summary>
    public class YTDLSeriesProvider : AbstractYoutubeRemoteProvider<YTDLSeriesProvider, Series, SeriesInfo> //IRemoteMetadataProvider<Series, SeriesInfo>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="TvdbSeriesProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{TvdbSeriesProvider}"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public YTDLSeriesProvider(
            IFileSystem fileSystem,
            ILogger<YTDLSeriesProvider> logger,
            IServerConfigurationManager config,
            ILibraryManager libraryManager,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, logger, config, afs)
        {
        }

        /// <inheritdoc />
        public override string Name => Constants.PluginName;

        public override async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            MetadataResult<Series> result = new();
            var id = info.Name;
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogInformation("No name found for media: ", info.Path);
                result.HasMetadata = false;
                return result;
            }
            var ytPath = GetVideoInfoPath(this._config.ApplicationPaths, id);
            var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
            if (!IsFresh(fileInfo))
            {
                await this.GetAndCacheMetadata(id, this._config.ApplicationPaths, cancellationToken);
            }
            var video = ReadYTDLInfo(ytPath, cancellationToken);
            if (video != null)
            {
                result = this.GetMetadataImpl(video);
            }
            return result;
        }

        internal override MetadataResult<Series> GetMetadataImpl(YTDLData jsonObj) => YTDLJsonToSeries(jsonObj);

        internal async override Task GetAndCacheMetadata(
            string name,
            IServerApplicationPaths appPaths,
            CancellationToken cancellationToken)
        {
            var ytPath = GetVideoInfoPath(this._config.ApplicationPaths, name);
            var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
            if (!IsFresh(fileInfo))
            {
                var searchResult = Utils.SearchChannel(name, appPaths, cancellationToken);
                await searchResult;
                await Utils.GetChannelInfo(searchResult.Result, name, appPaths, cancellationToken);

            }

        }
    }
}