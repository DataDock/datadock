using System;
using DataDock.Web.Models;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataDock.Web.Controllers
{
    public class DashboardBaseController : Controller
    {
        public string RequestedOwnerId { get; set; }
        public string RequestedRepoId { get; set; }

        public string RequestedDatasetId { get; set; }

        public DashboardViewModel DashboardViewModel { get; set; }

        protected ActionResult ReturnUnauthorizedView()
        {
            return View("Unauthorized");
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            // note - this does not work on postback
            const string key = "ownerId";
            var ownerId = context.ActionArguments.ContainsKey(key) ? context.ActionArguments[key] : "";
            if (ownerId == null)
            {
                context.Result = RedirectToAction("Index", "Home");
                return;
            }
            RequestedOwnerId = ownerId.ToString();

            const string rkey = "repoId";
            RequestedRepoId = "";
            var repoId = context.ActionArguments.ContainsKey(rkey) ? context.ActionArguments[rkey] : "";
            if(!repoId.ToString().Equals("repositories", StringComparison.InvariantCultureIgnoreCase)) RequestedRepoId = repoId.ToString();

            var dvm = new DashboardViewModel
            {
                SelectedOwnerId = RequestedOwnerId,
                SelectedRepoId = RequestedRepoId
            };
            DashboardViewModel = dvm;
        }
        
        public string GetSettingsStatusMessage(ManageMessageId? message = null)
        {
            if (message == null)
            {
                // check in TempData if a message isn't directly supplied
                message = TempData["message"] as ManageMessageId?;
            }
            var statusMessage = message == ManageMessageId.ChangeSettingSuccess
                ? @"The settings have been successfully updated."
                : message == ManageMessageId.ValidationError
                    ? @"There is a problem with missing or invalid information on this form."
                : message == ManageMessageId.Error
                    ? @"An error has occurred."
                    : message == ManageMessageId.TokenResetError ?
                        @"Unable to reset token." :
                        @"";
            return statusMessage;
        }
    }
}
