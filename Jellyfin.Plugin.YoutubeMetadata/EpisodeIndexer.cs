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
        public EpisodeIndexer(ILibraryManager libmanager, IItemRepository repository)
        {
            _libmanager = libmanager;
            _repository = repository;
        }
        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            //var foo = new InternalPeopleQuery();
            //foo.PersonTypes.Append(PersonType.Director);

            //foreach (var person in _libmanager.GetPeopleItems(foo))
            //{
            //if (person.HasProviderId(Constants.PluginName))
            //{
            //var query = new InternalItemsQuery();
            //var dto = new DtoOptions();
            //dto.Fields = new List<ItemFields>{ };
            //query.DtoOptions = dto;
            //var result = _libmanager.GetItemList(query);

            //}
            //var biz = "";
            //}
            var result = _repository.GetItems(new InternalItemsQuery
            {
                OrderBy = new[]
    {
        ("SortName", SortOrder.Ascending)
    },
                Recursive = true,
                IncludeItemTypes = new[] { BaseItemKind.Series },
                // 10.7 IncludeItemTypes = new[] { typeof(Series).FullName },
                DtoOptions = new DtoOptions()
            });
            var yoo = "a";
            //_libmanager.QueryItems();
            throw new NotImplementedException();
        }
    }
}
