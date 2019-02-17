using System.Threading.Tasks;
using DataDock.Common;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Web.Auth;
using DataDock.Web.Models;
using DataDock.Web.Routing;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DataDock.Web.Controllers
{
    public class DatasetController : DashboardBaseController
    {
        private readonly IDatasetStore _datasetStore;
        private readonly IJobStore _jobStore;
        private readonly IDataDockUriService _uriService;

        public DatasetController(IDatasetStore datasetStore, IJobStore jobStore, IDataDockUriService uriService)
        {
            _datasetStore = datasetStore;
            _jobStore = jobStore;
            _uriService = uriService;
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
        public async Task<IActionResult> DatasetVisibility(string ownerId, string repoId, string datasetId, string showOrHide)
        {
            this.DashboardViewModel.Area = "datasets";
            DashboardViewModel.SelectedDatasetId = datasetId;
            DashboardViewModel.Heading = string.Format("Delete Dataset: {0}", datasetId);

            var dataset =
                await _datasetStore.GetDatasetInfoAsync(ownerId, repoId, datasetId);
            if (string.IsNullOrEmpty(showOrHide))
            {
                // redirect back to admin without doing anything
                return View("Dashboard/Dataset", this.DashboardViewModel);
            }

            if (showOrHide.Equals("show")) dataset.ShowOnHomePage = true;
            if (showOrHide.Equals("hide")) dataset.ShowOnHomePage = false;

            await _datasetStore.CreateOrUpdateDatasetRecordAsync(dataset);
            return View("Dashboard/Dataset", this.DashboardViewModel);
        }

        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> DeleteDataset(string ownerId, string repoId, string datasetId, bool confirmed = false)
        {
            DashboardViewModel.Area = "datasets";
            DashboardViewModel.SelectedDatasetId = datasetId;
            DashboardViewModel.Heading = $"Delete Dataset: {datasetId}";
            if (!confirmed)
            {
                return View("Dashboard/DeleteDataset", this.DashboardViewModel);
            }

            // Validate user name and authentication status
            var userId = User?.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
            {
                Log.Debug("DeleteDataset: No value found for user principal name");
                return Unauthorized();
            }

            if (User?.Identity?.IsAuthenticated != true)
            {
                Log.Debug("DeleteDataset: User identity is not authenticated");
                return Unauthorized();
            }

            var jobInfo = await _jobStore.SubmitDeleteJobAsync(new DeleteJobRequestInfo
            {
                UserId = userId,
                OwnerId = ownerId,
                RepositoryId = repoId,
                DatasetId = datasetId,
                DatasetIri = _uriService.GetDatasetIdentifier(ownerId, repoId, datasetId)
            });
            if (jobInfo != null)
            {
                return RedirectToRoute("RepoJobs", new {ownerId, repoId, jobInfo.JobId});
            }
            ViewBag.StatusMessage = "Failed to delete dataset {datasetId}";
            return View("Dashboard/DeleteDataset", this.DashboardViewModel);
        }
    }
}