using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace DataDock.Web.Auth
{
    public static class ClaimExtensions
    {
        public static bool ClaimExists(this IPrincipal principal, string claimType)
        {
            var ci = principal as ClaimsPrincipal;
            if (ci == null)
            {
                return false;
            }

            var claim = ci.Claims.FirstOrDefault(x => x.Type == claimType);

            return claim != null;
        }

        public static bool HasClaim(this IPrincipal principal, string claimType, string claimValue, string issuer = null)
        {
            var ci = principal as ClaimsPrincipal;
            if (ci == null)
            {
                return false;
            }

            var claim = ci.Claims.FirstOrDefault(x => x.Type == claimType && x.Value == claimValue && (issuer == null || x.Issuer == issuer));

            return claim != null;
        }
    }
}
