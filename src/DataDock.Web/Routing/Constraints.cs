using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Linq;

namespace DataDock.Web.Routing
{

    public class OwnerIdConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            List<string> allowedPages = new List<string>()
            {
                "",
                "search",
                "account",
                "manage",
                "info",
                "import",
                "jobs",
                "job",
                "repositories",
                "datasets",
                "library",
                "progress",
                "loader"
            };
            var ownerId = values["ownerId"].ToString().ToLower();
            // Check for a match (assumes case insensitive)
            var match = allowedPages.Any(x => x.ToLower() == ownerId);
            return !match;
        }

    }

    public class RepoIdConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            var repoId = values["repoId"].ToString().ToLower();
            List<string> allowedPages = new List<string>()
            {
                "",
                "repositories",
                "library"
            };
            var match = allowedPages.Any(x => x.ToLower() == repoId);
            return !match;
        }

    }

}
