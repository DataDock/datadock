using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Controllers
{
    [Route("query/{owner}/{repo}/{dataset?}")]
    public class QueryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
