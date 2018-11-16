using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataDock.Common;
using DataDock.Common.Stores;
using DataDock.Web.Auth;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.ViewComponents
{
    public class DatasetViewComponent : ViewComponent
    {
        private readonly IDatasetStore _datasetStore;
        private readonly IDataDockUriService _uriService;

        public DatasetViewComponent(IDatasetStore datasetStore, IDataDockUriService uriService)
        {
            _datasetStore = datasetStore;
            _uriService = uriService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string ownerId, string repoId, string datasetId)
        {
            try
            {
                if (string.IsNullOrEmpty(ownerId) || string.IsNullOrEmpty(repoId) ||
                    string.IsNullOrEmpty(datasetId))
                {
                    return View("Empty");
                }

                var user = Request.HttpContext.User;
                var isAdmin =  user?.Identity.Name != null && user.Identity.IsAuthenticated &&
                                               ClaimsHelper.OwnerExistsInUserClaims(user.Identity as ClaimsIdentity, ownerId);
                var dataset =
                    await _datasetStore.GetDatasetInfoAsync(ownerId, repoId, datasetId);
                return View("Default", new DatasetViewModel(_uriService, dataset, isOwner:isAdmin));
            }
            catch (Exception e)
            {
                return View("Error", e);
            }
        }
    }
}
