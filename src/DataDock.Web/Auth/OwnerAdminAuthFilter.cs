using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace DataDock.Web.Auth
{
    public class OwnerAdminAuthFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;
            const string key = "ownerId";
            var ownerId = context.ActionArguments.ContainsKey(key) ? context.ActionArguments[key] : "";
            var authorized = user?.Identity.Name != null && user.Identity.IsAuthenticated &&
                             ClaimsHelper.OwnerExistsInUserClaims(user.Identity as ClaimsIdentity, ownerId.ToString());
            if (!authorized)
            {
                context.Result = new Http403Result();
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // n/a
        }
    }
}
