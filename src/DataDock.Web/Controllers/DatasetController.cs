using System.Threading.Tasks;
using DataDock.Common;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DataDock.Web.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(AccountExistsFilter))]
    [ServiceFilter(typeof(OwnerAdminAuthFilter))]
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


        /// <summary>
        /// View the details of a particular dataset
        /// </summary>
        /// <param name="ownerId">Dataset owner</param>
        /// <param name="repoId">Dataset repository</param>
        /// <param name="datasetId">Dataset ID</param>
        /// <returns></returns>
        public IActionResult Index(string ownerId, string repoId, string datasetId)
        {
            DashboardViewModel.Area = "dataset";
            DashboardViewModel.SelectedDatasetId = datasetId;
            DashboardViewModel.Title =
                $"{DashboardViewModel.SelectedOwnerId} / {DashboardViewModel.SelectedRepoId} Dataset";
            DashboardViewModel.Heading =
                $"Dataset: {DashboardViewModel.SelectedOwnerId} / {DashboardViewModel.SelectedRepoId}";
            return View("Dashboard/Dataset", this.DashboardViewModel);
        }

        public async Task<IActionResult> DatasetVisibility(string ownerId, string repoId, string datasetId, string showOrHide)
        {
            this.DashboardViewModel.Area = "datasets";
            DashboardViewModel.SelectedDatasetId = datasetId;
            DashboardViewModel.Heading = $"Dataset: {datasetId}";

            if (!string.IsNullOrEmpty(showOrHide))
            {
                var dataset =
                    await _datasetStore.GetDatasetInfoAsync(ownerId, repoId, datasetId);
                if (showOrHide.Equals("show")) dataset.ShowOnHomePage = true;
                if (showOrHide.Equals("hide")) dataset.ShowOnHomePage = false;
                await _datasetStore.CreateOrUpdateDatasetRecordAsync(dataset);
            }

            return RedirectToRoute("Dataset", new {ownerId, repoId, datasetId});
        }

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