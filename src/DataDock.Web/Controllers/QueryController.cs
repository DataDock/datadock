using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Controllers
{
    [Route("query/{owner}/{repo}/{dataset?}")]
    public class QueryController : Controller
    {
        public IActionResult Index([FromRoute] string owner, [FromRoute] string repo)
        {
            var viewModel = new QueryViewModel {DataSources = new string[] {$"http://localhost:5000/ldf/{owner}/{repo}"}};
            return View(viewModel);
        }
    }
}
