using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Common.Stores;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "RepositoryList")]
    public class RepositoryListViewComponent : ViewComponent
    {
        private readonly IRepoSettingsStore _repoSettingsStore;

        public RepositoryListViewComponent(IRepoSettingsStore repoSettingsStore)
        {
            _repoSettingsStore = repoSettingsStore;
        }

        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedOwnerId)) return View("Empty");

                var repos = await GetOwnerRepoSettings(selectedOwnerId);
                return View(repos);
            }
            catch (Exception e)
            {
                return View("Error", e);
            }
        }

        private async Task<List<RepoSettingsViewModel>> GetOwnerRepoSettings(string selectedOwnerId)
        {
            try
            {
                var repoSettings = await _repoSettingsStore.GetRepoSettingsForOwnerAsync(selectedOwnerId);
                var repoSettingsViewModels = repoSettings.Select(r => new RepoSettingsViewModel(r)).ToList();
                return repoSettingsViewModels;
            }
            catch (RepoSettingsNotFoundException rsnf)
            {
                Log.Warning($"No repository settings found for '{selectedOwnerId}'");
                return new List<RepoSettingsViewModel>();
            }
        }
    }
}
