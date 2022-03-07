using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YoutubeMetadata
{
    public class EpisodeIndexer : ILibraryPostScanTask, IScheduledTask
    {
        protected readonly ILibraryManager _libmanager;
        protected readonly IItemRepository _repository;
        protected readonly ILogger<EpisodeIndexer> _logger;
        public EpisodeIndexer(
            ILibraryManager libmanager,
            IItemRepository repository,
            ILogger<EpisodeIndexer> logger)
        {
            _libmanager = libmanager;
            _repository = repository;
            _logger = logger;
        }

        public string Name => Constants.PluginName;

        public string Key => Constants.PluginName;

        public string Description => Constants.PluginName;

        public string Category => Constants.PluginName;

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            await Run(progress, cancellationToken);
            return;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks }
            };
        }

        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting Reindexing episodes");
            var shows = _repository.GetItems(new InternalItemsQuery
            {
                Recursive = true,
                IncludeItemTypes = new[] { BaseItemKind.Series },
                DtoOptions = new DtoOptions()
            });
            var count = 0;
            foreach (var show in shows.Items)
            {
                if (!show.ProviderIds.ContainsKey(Constants.PluginName))
                {
                    _logger.LogDebug("Skipping show {Name}", show.Name);
                    continue;
                }
                _logger.LogDebug("Indexing show {Name}", show.Name);
                var seasons = new List<BaseItem>(_repository.GetItems(new InternalItemsQuery
                {
                    ParentId = show.Id,
                    IncludeItemTypes = new[] { BaseItemKind.Season },
                    DtoOptions = new DtoOptions()
                }).Items);
                seasons.Sort(delegate (BaseItem x, BaseItem y)
                {
                    if (x.Name == null && y.Name == null) return 0;
                    else if (x.Name == null) return -1;
                    else if (y.Name == null) return 1;
                    else return x.Name.CompareTo(y.Name);
                });
                var sindex = 1;
                foreach (var season in seasons)
                {
                    season.IndexNumber = sindex;
                    _logger.LogDebug("Indexing season {Name} as index {Index}", season.Name, sindex);
                    await _libmanager.UpdateItemAsync(season, show, ItemUpdateType.MetadataEdit, cancellationToken);
                    var episodes = new List<BaseItem>(_repository.GetItems(new InternalItemsQuery
                    {
                        AncestorIds = new[] { season.Id },
                        IncludeItemTypes = new[] { BaseItemKind.Episode },
                        DtoOptions = new DtoOptions()
                    }).Items);
                    episodes.Sort(delegate (BaseItem x, BaseItem y)
                    {
                        if (x.Name == null && y.Name == null) return 0;
                        else if (x.Name == null) return -1;
                        else if (y.Name == null) return 1;
                        else return x.Name.CompareTo(y.Name);
                    });
                    var eindex = 1;
                    foreach (var episode in episodes)
                    {
                        _logger.LogDebug("Epsiode {Name} should now be index {Index}", episode.Name, eindex);
                        episode.IndexNumber = eindex;
                        episode.ParentIndexNumber = sindex;
                        await _libmanager.UpdateItemAsync(episode, season, ItemUpdateType.MetadataEdit, cancellationToken);
                        eindex++;
                    }
                    sindex++;
                }
                count++;
                double percent = ((double)count / shows.Items.Count) * 100;
                progress.Report(percent);
            }
            return;
        }
    }
}
