using System;
using System.Collections.Generic;
using DataDock.Common.Models;
using DataDock.Web.Models;
using System.Linq;
using System.Security.Claims;
using Newtonsoft.Json;

namespace DataDock.Web.Auth
{
    public class ClaimsHelper
    {
        public OwnerInfo GetUserOwnerFromClaims(ClaimsIdentity identity)
        {
            if (identity == null) return null;
            var ghLoginClaim = identity.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.GitHubLogin));
            var ghAvatarClaim = identity.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.GitHubAvatar));
            if (ghLoginClaim != null)
            {
                var owner = new OwnerInfo {OwnerId = ghLoginClaim.Value};
                if (ghAvatarClaim != null)
                {
                    owner.AvatarUrl = ghAvatarClaim.Value;
                }

                return owner;
            }

            return null;
        }

        public static IEnumerable<OwnerInfo> GetOrgOwnersFromClaims(ClaimsIdentity identity)
        {
            var ghOrgClaims = identity?.Claims.Where(c => c.Type.Equals(DataDockClaimTypes.GitHubUserOrganization));
            return ghOrgClaims?.Select(claim => JsonConvert.DeserializeObject<OwnerInfo>(claim.Value)).ToList();
        }

        /// <summary>
        /// In order to have edit access to an owner (user or org) or its child repos, the owner must exist as a claim in the authorized identity
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public static bool OwnerExistsInUserClaims(ClaimsIdentity identity, string ownerId)
        {
            var userClaim = identity?.Claims.FirstOrDefault(c =>
                c.Type.Equals(DataDockClaimTypes.GitHubLogin) && c.Value != null && c.Value.Equals(ownerId, StringComparison.InvariantCultureIgnoreCase));
            if (userClaim != null) return true;
            var ownerOrg = GetOrgOwnersFromClaims(identity)?.FirstOrDefault(o => o.OwnerId.Equals(ownerId, StringComparison.InvariantCultureIgnoreCase));
            return ownerOrg != null;
        }
    }
}
