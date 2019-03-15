using System;
using DataDock.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DataDock.Common.Stores;
using DataDock.Web.Filters;
using DataDock.Web.Models;
using DataDock.Web.ViewModels;
using Microsoft.IdentityModel.Protocols;
using Serilog;
using Microsoft.Extensions.Configuration;

namespace DataDock.Web.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(AccountExistsFilter))]
    public class RepositoryController : DashboardBaseController
    {
        private readonly IRepoSettingsStore _repoSettingsStore;
        private readonly IConfiguration _configuration;

        public RepositoryController(IRepoSettingsStore repoSettingsStore, IConfiguration configuration)
        {
            _repoSettingsStore = repoSettingsStore;
            _configuration = configuration;
        }

        /// <summary>
        /// User or Org summary of data uploads to a partcular repo
        /// Viewable by public and other DataDock users as well as authorized users
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(string ownerId, string repoId)
        {
            return await Task.Run(() =>
            {
                DashboardViewModel.Area = "summary";
                DashboardViewModel.Title =
                    $"{DashboardViewModel.SelectedOwnerId} > {DashboardViewModel.SelectedRepoId} Summary";
                return View("Dashboard/Index", DashboardViewModel);
            });
        }

        /// <summary>
        /// User or Org dataset uploads to a particular repo
        /// Viewable by public and other DataDock users as well as authorized users
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Datasets(string ownerId, string repoId)
        {
            return await Task.Run(() =>
            {
                DashboardViewModel.Area = "datasets";
                DashboardViewModel.Title =
                    $"{DashboardViewModel.SelectedOwnerId} > {DashboardViewModel.SelectedRepoId} Datasets";
                return View("Dashboard/Datasets", DashboardViewModel);
            });
        }

        /// <summary>
        /// User or Org template library for a particular repo
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Library(string ownerId, string repoId)
        {
            return await Task.Run(() =>
            {
                DashboardViewModel.Area = "library";
                DashboardViewModel.Title =
                    $"{DashboardViewModel.SelectedOwnerId} > {DashboardViewModel.SelectedRepoId} Template Library";
                return View("Dashboard/Library", DashboardViewModel);
            });
        }

        /// <summary>
        /// Add data to an org or user github to a particular repo
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <param name="schemaId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        [GenerateAntiforgeryTokenCookieForAjax]
        public async Task<IActionResult> Import(string ownerId, string repoId, string schemaId = "")
        {
            return await Task.Run(() =>
            {
                DashboardViewModel.Area = "import";
                DashboardViewModel.Title =
                    $"{DashboardViewModel.SelectedOwnerId} > {DashboardViewModel.SelectedRepoId} Add Data";

                ViewData["OwnerId"] = DashboardViewModel.SelectedOwnerId;
                ViewData["RepoId"] = DashboardViewModel.SelectedRepoId;
                ViewData["SchemaId"] = schemaId;
                ViewData["BaseUrl"] = _configuration["BaseUrl"];
                ViewData["PublishUrl"] = _configuration["PublishUrl"];

                DashboardViewModel.SelectedSchemaId = schemaId;
                return View("Import", DashboardViewModel);
            });
        }

        /// <summary>
        /// job history list for a partcular repo
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <param name="jobId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Jobs(string ownerId, string repoId, string jobId = "")
        {
            return await Task.Run(() =>
            {
                DashboardViewModel.Area = "jobs";
                DashboardViewModel.Title =
                    $"{DashboardViewModel.SelectedOwnerId} > {DashboardViewModel.SelectedRepoId} Job History";
                ViewData["JobId"] = jobId;
                return View("Dashboard/Jobs", DashboardViewModel);
            });
        }

        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Job(string ownerId, string repoId, string jobId)
        {
            return await Task.Run(() =>
            {
                DashboardViewModel.Area = "jobs";
                DashboardViewModel.Title =
                    $"{DashboardViewModel.SelectedOwnerId} > {DashboardViewModel.SelectedRepoId} Job Log";
                ViewData["JobId"] = jobId;

                // get log ID, view job log

                return View("Dashboard/Job", DashboardViewModel);
            });
        }

        /// <summary>
        /// org/user settings for a particular repo
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Settings(string ownerId, string repoId)
        {
            return await Task.Run(() =>
            {
                DashboardViewModel.Area = "settings";
                DashboardViewModel.Title =
                    $"{DashboardViewModel.SelectedOwnerId} > {DashboardViewModel.SelectedRepoId} Settings";
                return View("Settings", DashboardViewModel);
            });
        }

        [HttpPost]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Settings(string ownerId, string repoId, RepoSettingsViewModel settingsViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    settingsViewModel.LastModified = DateTime.UtcNow;
                    settingsViewModel.LastModifiedBy = User.Identity.Name;
                    if (!ownerId.Equals(User.Identity.Name)) settingsViewModel.OwnerIsOrg = true;
                    var repoSettings = settingsViewModel.AsRepoSettings();
                    await _repoSettingsStore.CreateOrUpdateRepoSettingsAsync(repoSettings);
                    ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.ChangeSettingSuccess);
                    TempData["ModelState"] = null;
                }
                catch (Exception e)
                {
                    ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.Error);
                    Log.Error(e, "Error updating repo settings for '{0}/{1}'", ownerId, repoId);
                    throw;
                }
            }
            else
            {
                // pass errors to the ViewComponent
                ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.ValidationError);
                TempData["ModelState"] = ModelState;
                TempData["ViewModel"] = settingsViewModel;
            }
            
            DashboardViewModel.Area = "settings";
            DashboardViewModel.Title = string.Format("{0} Settings", DashboardViewModel.SelectedOwnerId, DashboardViewModel.SelectedRepoId);
            return View("Settings", DashboardViewModel);
        }

        
    }
}