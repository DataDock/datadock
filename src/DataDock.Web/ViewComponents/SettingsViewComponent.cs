using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Serilog;
using System;
using System.Threading.Tasks;
using DataDock.Common.Stores;

namespace DataDock.Web.ViewComponents
{
    public class SettingsViewComponent : ViewComponent
    {
        private readonly IOwnerSettingsStore _ownerSettingsStore;
        private readonly IRepoSettingsStore _repoSettingsStore;
        public SettingsViewComponent(IOwnerSettingsStore ownerSettingsStore, IRepoSettingsStore repoSettingsStore)
        {
            _ownerSettingsStore = ownerSettingsStore;
            _repoSettingsStore = repoSettingsStore;
        }

        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string selectedRepoId)
        {
            if (string.IsNullOrEmpty(selectedOwnerId)) return View("Empty");

            try
            {
                var loadSettings = true;
                // Validation
                if (TempData["ModelState"] is ModelStateDictionary modelState)
                {
                    if (!modelState.IsValid)
                    {
                        // display errors
                        loadSettings = false;
                    }
                }
                if (string.IsNullOrEmpty(selectedRepoId))
                {
                    var osvm = loadSettings ? await GetOwnerSettingsViewModel(selectedOwnerId) : TempData["ViewModel"];
                    return View("Owner", osvm);
                }

                var rsvm = loadSettings ? await GetRepoSettingsViewModel(selectedOwnerId, selectedRepoId) : TempData["ViewModel"];
                return View("Repo", rsvm);


            }
            catch (Exception e)
            {
                return View("Error", e);
            }
           
        }

        private async Task<OwnerSettingsViewModel> GetOwnerSettingsViewModel(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId)) return null;
            try
            {
                var os = await _ownerSettingsStore.GetOwnerSettingsAsync(ownerId);
                var osvm = new OwnerSettingsViewModel(os);
                return osvm;
            }
            catch (OwnerSettingsNotFoundException notFound)
            {
                Log.Debug("No owner settings found for owner '{0}'", ownerId);
                return new OwnerSettingsViewModel {OwnerId = ownerId};
            }
            catch (Exception e)
            {
                Log.Error(e, "Error retrieving owner settings with owner id '{0}'", ownerId);
                throw;
            }
        }

        private async Task<RepoSettingsViewModel> GetRepoSettingsViewModel(string ownerId, string repoId)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException();
            if (string.IsNullOrEmpty(repoId)) throw new ArgumentNullException();

            try
            {
                var rs = await _repoSettingsStore.GetRepoSettingsAsync(ownerId, repoId);
                var rsvm = new RepoSettingsViewModel(rs);
                return rsvm;
            }
            catch (RepoSettingsNotFoundException notFound)
            {
                Log.Debug("No repo settings found for repo '{0}/{1}'", ownerId, repoId);
                return new RepoSettingsViewModel { OwnerId = ownerId, RepoId = repoId };
            }
            catch (Exception e)
            {
                Log.Error(e, "Error retrieving owner settings with owner id '{0}'", ownerId);
                throw;
            }
        }
    }
}
