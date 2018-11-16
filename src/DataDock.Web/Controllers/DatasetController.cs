using System.Threading.Tasks;
using DataDock.Common.Stores;
using DataDock.Web.Auth;
using DataDock.Web.Models;
using DataDock.Web.Routing;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Controllers
{
    public class DatasetController : DashboardBaseController
    {
        private IDatasetStore _datasetStore;

        public DatasetController(IDatasetStore datasetStore)
        {
            _datasetStore = datasetStore;
        }


        // GET
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public IActionResult Index(string ownerId, string repositoryId, string datasetId)
        {
            DashboardViewModel.Area = "dataset";
            DashboardViewModel.SelectedDatasetId = datasetId;
            DashboardViewModel.Title = string.Format("{0} > {1} Dataset", DashboardViewModel.SelectedOwnerId,
                DashboardViewModel.SelectedRepoId);
            return View("Dashboard/Dataset", this.DashboardViewModel);
        }

        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> DeleteDataset(string ownerId, string repoId, string datasetId, bool confirmed = false)
        {
            this.DashboardViewModel.Area = "datasets";
            DashboardViewModel.SelectedDatasetId = datasetId;
            DashboardViewModel.Heading = string.Format("Delete Dataset: {0}", datasetId);
            if (!confirmed)
            {
                return View("Dashboard/DeleteDataset", this.DashboardViewModel);
            }
            else
            {
                if (await _datasetStore.DeleteDatasetAsync(ownerId, repoId, datasetId))
                {
                    TempData["message"] = $"Dataset {datasetId} successfully deleted.";
                    return RedirectToRoute("RepoDatasets", new {ownerId, repoId});
                }
                else
                {
                    ViewBag.StatusMessage = "Failed to delete dataset {datasetId}";
                    return View("Dashboard/DeleteDataset", this.DashboardViewModel);
                }
            }
        }
    }
}