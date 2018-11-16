using DataDock.Common.Stores;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Common;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "Datasets")]
    public class DatasetsViewComponent : ViewComponent
    {
        private readonly IDatasetStore _datasetStore;
        private readonly IDataDockUriService _uriService;

        public DatasetsViewComponent(IDatasetStore datasetStore, IDataDockUriService uriService)
        {
            _datasetStore = datasetStore;
            _uriService = uriService;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string selectedRepoId)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedOwnerId)) return View("Empty");

                if (string.IsNullOrEmpty(selectedRepoId))
                {
                    var datasetsList = await GetOwnerDatasets(selectedOwnerId);
                    return View("Default", datasetsList);
                }
                var repoDatasetsList = await GetRepoDatasets(selectedOwnerId, selectedRepoId);
                return View("Default", repoDatasetsList);
            }
            catch (Exception e)
            {
                return View("Error", e);
            }
          
        }

        private async Task<List<DatasetViewModel>> GetOwnerDatasets(string selectedOwnerId)
        {
            try
            {
                var datasets = await _datasetStore.GetDatasetsForOwnerAsync(selectedOwnerId, 0, 20);
                var datasetViewModels = datasets.Select(d => new DatasetViewModel(_uriService, d)).ToList();
                return datasetViewModels;
            }
            catch (DatasetNotFoundException)
            {
                return new List<DatasetViewModel>();
            }
            
        }

        private async Task<List<DatasetViewModel>> GetRepoDatasets(string selectedOwnerId, string selectedRepoId)
        {
            try
            {
                var datasets = await _datasetStore.GetDatasetsForRepositoryAsync(selectedOwnerId, selectedRepoId, 0, 20);
                var datasetViewModels = datasets.Select(d => new DatasetViewModel(_uriService, d)).ToList();
                return datasetViewModels;
            }
            catch (DatasetNotFoundException)
            {
                return new List<DatasetViewModel>();
            }
            
        }
    }
}
