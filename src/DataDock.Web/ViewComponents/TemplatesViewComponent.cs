using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Common.Stores;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "Templates")]
    public class TemplatesViewComponent : ViewComponent
    {
        private readonly ISchemaStore _schemaStore;

        public TemplatesViewComponent(ISchemaStore schemaStore)
        {
            _schemaStore = schemaStore;
        }

        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string selectedRepoId)
        {
            try
            {
                ViewBag.OwnerId = selectedOwnerId;
                ViewBag.RepoId = selectedRepoId;
                if (string.IsNullOrEmpty(selectedOwnerId)) return View("Empty");

                if (string.IsNullOrEmpty(selectedRepoId))
                {
                    var templatesList = await GetTemplatesForOwner(selectedOwnerId);
                    return View(templatesList);
                }
                var repoTemplatesList = await GetTemplatesForRepository(selectedOwnerId, selectedRepoId);
                return View(repoTemplatesList);
            }
            catch (Exception e)
            {
                return View("Error", e);
            }
        }

        private async Task<List<TemplateViewModel>> GetTemplatesForOwner(string ownerId)
        {
            try
            {
                var schemas = _schemaStore.GetSchemasByOwner(ownerId, 0, 20);
                return schemas?.Select(s => new TemplateViewModel(s)).ToList();
            }
            catch (SchemaNotFoundException snf)
            {
                return new List<TemplateViewModel>();
            }
        }

        private async Task<List<TemplateViewModel>> GetTemplatesForRepository(string ownerId, string repositoryId)
        {
            try
            {
                var schemas = _schemaStore.GetSchemasByRepositoryList(ownerId, new string[]{repositoryId}, 0, 20);
                return schemas?.Select(s => new TemplateViewModel(s)).ToList();
            }
            catch (SchemaNotFoundException snf)
            {
                return new List<TemplateViewModel>();
            }
        }
    }
}
