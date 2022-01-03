﻿using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller;
using System.IO;
using System.Text.Json;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YTDLMusicProvider : AbstractYoutubeRemoteProvider<YTDLMusicProvider, MusicVideo, MusicVideoInfo>
    {
        public YTDLMusicProvider(
            IFileSystem fileSystem,
            ILogger<YTDLMusicProvider> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, logger, config, afs)
        {
        }

        public override string Name => Constants.PluginName;

        internal override MetadataResult<MusicVideo> GetMetadataImpl(YTDLData jsonObj, string id) => YTDLJsonToMusicVideo(jsonObj, id);

        internal async override Task GetAndCacheMetadata(
            string id,
            IServerApplicationPaths appPaths,
            CancellationToken cancellationToken)
        {
            await Utils.YTDLMetadata(id, appPaths, cancellationToken);
        }
    }
}