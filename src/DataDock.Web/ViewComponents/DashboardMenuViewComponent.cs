using System;
using System.Linq;
using System.Security.Claims;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DataDock.Common.Stores;
using DataDock.Web.Auth;
using DataDock.Web.Models;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "DashboardMenu")]
    public class DashboardMenuViewComponent : ViewComponent
    {
        private readonly IRepoSettingsStore _repoSettingsStore;

        public DashboardMenuViewModel DashboardMenuViewModel { get; set; }

        public DashboardMenuViewComponent(IRepoSettingsStore repoSettingsStore)
        {
            _repoSettingsStore = repoSettingsStore;
        }

        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string selectedRepoId, string area)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated || !ClaimsHelper.OwnerExistsInUserClaims(User.Identity as ClaimsIdentity, selectedOwnerId))
            {
                // dash view model
                var publicDash = new DashboardMenuViewModel
                {
                    SelectedOwnerId = selectedOwnerId,
                    SelectedRepoId = selectedRepoId,
                    ActiveArea = area
                };
                //note: when public, the avatar URL cannot be retrieved from the user claims, so needs to be retrieved from data storage / cache
                return View("Public", publicDash);
            }
            
            // user view model
            var uvm = new UserViewModel();
            uvm.Populate(User.Identity as ClaimsIdentity);
                
            // dash view model
            this.DashboardMenuViewModel = new DashboardMenuViewModel
            {
                SelectedOwnerId = selectedOwnerId,
                SelectedRepoId = selectedRepoId,
                UserViewModel = uvm,
                ActiveArea = area
            };
            this.DashboardMenuViewModel.Owners.Add(uvm.UserOwner);
            this.DashboardMenuViewModel.Owners.AddRange(uvm.Organisations);
            this.DashboardMenuViewModel.SelectedOwnerAvatarUrl = this.DashboardMenuViewModel.Owners.FirstOrDefault(o => o.OwnerId.Equals(selectedOwnerId, StringComparison.InvariantCultureIgnoreCase))?.AvatarUrl;
            await PopulateRepositoryList();
            return View(this.DashboardMenuViewModel);

        }

        private async Task PopulateRepositoryList()
        {
            if(string.IsNullOrEmpty(DashboardMenuViewModel?.SelectedOwnerId)) return;
            try
            {
                var repos = await _repoSettingsStore.GetRepoSettingsForOwnerAsync(this.DashboardMenuViewModel.SelectedOwnerId);
                foreach (var r in repos)
                {
                    var ri = new RepositoryInfo(r);
                    DashboardMenuViewModel.Owners.FirstOrDefault(o => o.OwnerId.Equals(this.DashboardMenuViewModel.SelectedOwnerId))?.Repositories.Add(ri);
                }
            }
            catch (RepoSettingsNotFoundException rnf)
            {
                // do nothing
                return;
            }
        }
    }
}
