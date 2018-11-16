using DataDock.Common.Stores;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Threading.Tasks;
using DataDock.Common;
using DataDock.Common.Models;

namespace DataDock.Web.Controllers
{
    public class SearchController : Controller
    {
        private readonly IDatasetStore _datasetStore;
        private readonly IDataDockUriService _uriService;

        public SearchController(IDatasetStore datasetStore, IDataDockUriService uriService)
        {
            _datasetStore = datasetStore;
            _uriService = uriService;
        }

        public async Task<IActionResult> Index(string tag, int skip = 0, int take = 25)
        {
            var model = new SearchResultViewModel(_uriService);
            try
            {
                if (!string.IsNullOrEmpty(tag))
                {
                    var tags = new [] {tag};
                    var results = await _datasetStore.GetDatasetsForTagsAsync(tags, skip, take);
                    model = new SearchResultViewModel(tag, results);
                }
            }
            catch (DatasetNotFoundException)
            {
                model = new SearchResultViewModel(tag, new DatasetInfo[0]);
            }
            catch (Exception e)
            {
                ViewBag.Error = e;
                Log.Error("Error searching by tag via the search page", e);
            }
            return View(model);
        }
    }
}