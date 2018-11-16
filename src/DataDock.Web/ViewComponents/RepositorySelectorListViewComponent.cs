using DataDock.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataDock.Web.Models;
using DataDock.Web.ViewModels;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "RepositorySelectorList")]
    public class RepositorySelectorListViewComponent : ViewComponent
    {
        private readonly IGitHubApiService _gitHubApiService;

        public RepositorySelectorListViewComponent(IGitHubApiService gitHubApiService)
        {
            _gitHubApiService = gitHubApiService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string selectedSchemaId, string display = "dropdown")
        {
            try
            {
                if (string.IsNullOrEmpty(selectedOwnerId)) return View("Empty");
                if (User?.Identity == null || !User.Identity.IsAuthenticated) return View("Empty");

                var repos = await GetRepositoriesForOwner(selectedOwnerId, selectedSchemaId);
                switch (display)
                {
                    case "link-list":
                        // shown when selecting a repo during import process
                        return View("DividedList", repos);
                    default:
                        // shown on repostories.cshtml in "add repo" dropdown selector
                        return View(repos);
                }
            }
            catch (Exception e)
            {
                return View("Error", e);
            }
        }

        public async Task<List<RepositoryInfo>> GetRepositoriesForOwner(string ownerId, string schemaId)
        {
            var repoInfos = new List<RepositoryInfo>();
            var allRepositories = await _gitHubApiService.GetRepositoryListForOwnerAsync(User.Identity, ownerId);
            foreach (var r in allRepositories)
            {
                repoInfos.Add(new RepositoryInfo(r, schemaId));
            }
            return repoInfos;
        }
    }
}
