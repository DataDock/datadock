using DataDock.Common.Stores;
using DataDock.Web.Auth;
using DataDock.Web.Services;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DataDock.Web.Controllers
{
    [Authorize(Policy = "Admin")]
    [ServiceFilter(typeof(AccountExistsFilter))]
    public class ManageController : Controller
    {
        private readonly IDatasetStore _datasetStore;
        private readonly ISchemaStore _schemaStore;
        private IHubContext<ProgressHub> _progresseHubContext;


        public ManageController(IDatasetStore datasetStore, 
            ISchemaStore schemaStore, 
            IHubContext<ProgressHub> progresseHubContext)
        {
            _datasetStore = datasetStore;
            _schemaStore = schemaStore;
            _progresseHubContext = progresseHubContext;
        }

        public IActionResult Index()
        {
            var model = new BaseLayoutViewModel {Title = "DataDock Admin"};
            model.Heading = model.Title;
            return View(model);
        }

        
        
    }
}