using DataDock.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataDock.Web.ViewModels
{
    /// <summary>
    /// The dashboard menu displays a dropdown list of owners that the user has access rights to (set during login in claims)
    /// It also displays links to repositories / datasets / templates / add data and settings for that owner
    /// </summary>
    public class DashboardMenuViewModel
    {
        public string SelectedOwnerId { get; set; }
        public string SelectedRepoId { get; set; }

        public string SelectedOwnerAvatarUrl { get; set; }
        public string ActiveArea { get; set; }

        public List<OwnerInfo> Owners { get; set; }

        public UserViewModel UserViewModel { get; set; }

        public DashboardMenuViewModel()
        {
            Owners = new List<OwnerInfo>();
        }

        /// <summary>
        /// return either {ownerId} or {ownerId}/{repoId} for use in link URLs
        /// </summary>
        /// <returns></returns>
        public string GetDashContext()
        {
            var dashContext = string.IsNullOrEmpty(SelectedRepoId) ? SelectedOwnerId : string.Format("{0}/{1}", SelectedOwnerId, SelectedRepoId);
            return dashContext.ToLower();
        }

        /// <summary>
        /// return CSS class name(s) to be used on menu items
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public string AreaIsActive(string area)
        {
            if (string.IsNullOrEmpty(area)) return string.Empty;
            return area.Equals(ActiveArea, StringComparison.InvariantCultureIgnoreCase) ? "active" : string.Empty;
        }

        public OwnerInfo GetActiveOwner()
        {
            var owner = this.Owners.FirstOrDefault(o =>
                o.OwnerId.Equals(this.SelectedOwnerId, StringComparison.InvariantCultureIgnoreCase));
            return owner;
        }
    }
}
