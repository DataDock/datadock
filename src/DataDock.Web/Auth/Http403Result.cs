using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Auth
{
    internal class Http403Result : ActionResult
    {
        public override void ExecuteResult(ActionContext context)
        {
            // Set the response code to 403.
            context.HttpContext.Response.StatusCode = 403;
        }
    }
}
