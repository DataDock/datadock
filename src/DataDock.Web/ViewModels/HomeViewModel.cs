using DataDock.Common.Models;
using System.Collections.Generic;
using System.Linq;
using DataDock.Common;

namespace DataDock.Web.ViewModels 
{
    public class HomeViewModel : BaseLayoutViewModel
    {
        private readonly IDataDockUriService _uriService;

        public HomeViewModel(IDataDockUriService uriService)
        {
            _uriService = uriService;
        }

        public IReadOnlyList<DatasetViewModel> RecentDatasets { get; }

        public HomeViewModel(IEnumerable<DatasetInfo> recentDatasets)
        {
            RecentDatasets = recentDatasets?.Select(ds => new DatasetViewModel(_uriService, ds)).ToList();
        }
    }
}
