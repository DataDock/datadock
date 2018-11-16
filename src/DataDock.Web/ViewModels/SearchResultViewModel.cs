using DataDock.Common.Models;
using System.Collections.Generic;
using System.Linq;
using DataDock.Common;

namespace DataDock.Web.ViewModels
{
    public class SearchResultViewModel : BaseLayoutViewModel
    {
        public string SearchTag { get; private set; }

        public IReadOnlyList<DatasetViewModel> Results { get; private set; }

        private readonly IDataDockUriService _uriService;

        public SearchResultViewModel(IDataDockUriService uriService)
        {
            _uriService = uriService;
        }

        public SearchResultViewModel(string tag, IEnumerable<DatasetInfo> searchResults)
        {
            SearchTag = tag;
            Results = searchResults.Select(d => new DatasetViewModel(_uriService, d)).ToList();
        }
    }
}
