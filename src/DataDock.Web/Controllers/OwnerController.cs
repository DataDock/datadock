using System;
using System.Net;
using DataDock.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DataDock.Common.Stores;
using DataDock.Web.Models;
using DataDock.Web.Services;
using DataDock.Web.ViewModels;
using Serilog;

namespace DataDock.Web.Controllers
{
    
    public class OwnerController : DashboardBaseController
    {
        private readonly IOwnerSettingsStore _ownerSettingsStore;
        private readonly IImportService _importService;
        private readonly ISchemaStore _schemaStore;

        public OwnerController(IOwnerSettingsStore ownerSettingsStore, IImportService importService, ISchemaStore schemaStore)
        {
            _ownerSettingsStore = ownerSettingsStore;
            _importService = importService;
            _schemaStore = schemaStore;
        }

        /// <summary>
        /// User or Org summary of data uploads
        /// Viewable by public and other DataDock users as well as authorized users
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(string ownerId = "")
        {
            this.DashboardViewModel.Area = "summary";
             DashboardViewModel.Title = string.Format("{0} Summary", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Index", this.DashboardViewModel);
        }

        /// <summary>
        /// User or Org repository list
        /// Viewable by public and other DataDock users as well as authorized users
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Repositories(string ownerId = "")
        {
            this.DashboardViewModel.Area = "repositories";
            DashboardViewModel.Title = string.Format("{0} Repos", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Repositories", this.DashboardViewModel);
        }

        /// <summary>
        /// User or Org dataset uploads
        /// Viewable by public and other DataDock users as well as authorized users
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Datasets(string ownerId = "")
        {
            this.DashboardViewModel.Area = "datasets";
            DashboardViewModel.Title = string.Format("{0} Datasets", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Datasets", this.DashboardViewModel);
        }

        /// <summary>
        /// User or Org template library
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Library(string ownerId = "")
        {
            this.DashboardViewModel.Area = "library";
            DashboardViewModel.Title = string.Format("{0} Template Library", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Library", this.DashboardViewModel);
        }

        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> UseSchema(string ownerId, string schemaId)
        {
            return RedirectToAction("Import", new {ownerId = ownerId, schemaId = schemaId});
        }

        public async Task<ActionResult> DeleteSchema(string ownerId, string schemaId)
        {
            if (string.IsNullOrEmpty(ownerId)) return new NotFoundResult();
            if (string.IsNullOrEmpty(schemaId)) return new NotFoundResult();

            if (User?.Identity == null || !User.Identity.IsAuthenticated) return new UnauthorizedResult();
            var model = new TemplateDeleteModel(schemaId);

            try
            {
                model.Area = "delete";
                model.SelectedOwnerId = ownerId;
                model.SchemaId = schemaId;
                model.UserId = User.Identity.Name;

                // check user has permission to admin
                var isAdmin = await _importService.CheckUserIsAdminOfOwner(User, ownerId);
                if (!isAdmin) return ReturnUnauthorizedView();

                var schemaInfo = await _schemaStore.GetSchemaInfoAsync(ownerId, schemaId);
                if (schemaInfo == null)
                {
                    Log.Warning(
                        "Schema validation failed. Could not find a schema info record for schema {0} for owner {1}",
                        model.SchemaId, model.SelectedOwnerId);
                    return new NotFoundResult();
                }
                model.SchemaInfo = schemaInfo;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading delete schema page");
                ModelState.AddModelError("", "Error encountered while loading the page");
            }
            return View(model);
        }

        // POST: /{ownerId}/{repoId}/{datasetId}/delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteSchema(TemplateDeleteModel model, string returnUrl)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                Log.Debug("/{0}/library/{1}/delete: attempting to delete template by user '{2}'", model.SelectedOwnerId, model.SchemaId, model.UserId);

                // check current user has permission to admin the dataset
                var isAdmin = await _importService.CheckUserIsAdminOfOwner(User, model.SelectedOwnerId);
                if (!isAdmin) return ReturnUnauthorizedView();

                if (string.IsNullOrEmpty(model.SchemaId))
                {
                    return new BadRequestResult();
                }

                var schemaInfo = await _schemaStore.GetSchemaInfoAsync(model.SelectedOwnerId, model.SchemaId);
                if (schemaInfo == null)
                {
                    Log.Warning(
                        "Schema validation failed. Could not find a schema info record for schema {0} for owner {1}",
                        model.SchemaId, model.SelectedOwnerId);
                    return new NotFoundResult();
                }
                model.SchemaInfo = schemaInfo;

                try
                {
                    await _schemaStore.DeleteSchemaAsync(model.SelectedOwnerId, model.SchemaId);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Unexpected error when attempting to delete schema at /{0}/library/{1}/delete", model.SelectedOwnerId, model.SchemaId);
                    model.HasErrored = true;
                    model.Errors.Add(string.Format("Unexpected error when attempting to delete template '{0}'", model.SchemaId));
                    return View("DeleteSchema", model);
                }


                return RedirectToAction("Library", new { ownerId = model.SelectedOwnerId });
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error when attempting to retrieve schema for deletion at /{0}/library/{1}/delete", model.SelectedOwnerId, model.SchemaId);
                model.HasErrored = true;
                model.Errors.Add(string.Format("Unexpected error when attempting to retrieve template for deletion '{0}'", model.SchemaId));
                return View("DeleteSchema", model);
            }
        }

        /// <summary>
        /// Add data to an org or user github
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="schemaId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Import(string ownerId = "", string schemaId = "")
        {
            this.DashboardViewModel.Area = "import";
            DashboardViewModel.Title = string.Format("{0} Add Data", DashboardViewModel.SelectedOwnerId);
            DashboardViewModel.SelectedSchemaId = schemaId;
            return View("Dashboard/Import", this.DashboardViewModel);
        }

        /// <summary>
        /// job history list
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Jobs(string ownerId = "")
        {
            this.DashboardViewModel.Area = "jobs";
            DashboardViewModel.Title = string.Format("{0} Job History", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Jobs", this.DashboardViewModel);
        }

        /// <summary>
        /// org/user settings
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Settings(string ownerId = "")
        {
            ViewBag.StatusMessage = GetSettingsStatusMessage();
            this.DashboardViewModel.Area = "settings";
            DashboardViewModel.Title = string.Format("{0} Settings", DashboardViewModel.SelectedOwnerId);
            return View("Settings", this.DashboardViewModel);
        }

        [HttpPost]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Settings(string ownerId, OwnerSettingsViewModel settingsViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    settingsViewModel.LastModified = DateTime.UtcNow;
                    settingsViewModel.LastModifiedBy = User.Identity.Name;
                    if (!ownerId.Equals(User.Identity.Name)) settingsViewModel.OwnerIsOrg = true;
                    var ownerSettings = settingsViewModel.AsOwnerSettings();
                    await _ownerSettingsStore.CreateOrUpdateOwnerSettingsAsync(ownerSettings);
                    ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.ChangeSettingSuccess);
                    TempData["ModelState"] = null;
                }
                catch (Exception e)
                {
                    ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.Error);
                    Log.Error(e, "Error updating owner settings for '{0}'", ownerId);
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

            this.DashboardViewModel.Area = "settings";
            DashboardViewModel.Title = string.Format("{0} Settings", DashboardViewModel.SelectedOwnerId);
            return View("Settings", this.DashboardViewModel);
        }
    }
}