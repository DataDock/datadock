using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Web.Auth;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated && !User.ClaimExists(DataDockClaimTypes.DataDockUserId))
            {
                return RedirectToAction("SignUp", "Account");
            }

            var userViewModel = new UserViewModel {Title = "DataDock"};
            userViewModel.Populate(User.Identity as ClaimsIdentity);
            return View(userViewModel);
        }

        public IActionResult Error()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }
    }
}
