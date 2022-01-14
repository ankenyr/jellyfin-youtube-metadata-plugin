using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YoutubeMetadata
{
    public class EpisodeIndexer : ILibraryPostScanTask
    {
        protected readonly ILibraryManager _libmanager;
        protected readonly IItemRepository _repository;
        public EpisodeIndexer(
            ILibraryManager libmanager,
            IItemRepository repository)
        {
            _libmanager = libmanager;
            _repository = repository;
        }
        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            
            var shows = _repository.GetItems(new InternalItemsQuery
            {
                Recursive = true,
                IncludeItemTypes = new[] { BaseItemKind.Series },
                DtoOptions = new DtoOptions()
            });
            foreach (var show in shows.Items)
            {
                if (!show.ProviderIds.ContainsKey(Constants.PluginName))
                {
                    continue;
                }
                var seasons = _repository.GetItems(new InternalItemsQuery
                {
                    ParentId = show.Id,
                    IncludeItemTypes = new[] { BaseItemKind.Season },
                    DtoOptions = new DtoOptions()
                });
                foreach (var season in seasons.Items)
                {
                    var episodes = _repository.GetItems(new InternalItemsQuery
                    {
                        AncestorIds = new[] { season.Id },
                        IncludeItemTypes = new[] { BaseItemKind.Episode },
                        DtoOptions = new DtoOptions()
                    });
                    var index = 1;
                    foreach (var episode in episodes.Items)
                    {
                        episode.IndexNumber = index;
                        await _libmanager.UpdateItemAsync(episode, season, ItemUpdateType.MetadataEdit, cancellationToken);
                        index++;
                    }
                }
            }
            return;
        }
    }
}
