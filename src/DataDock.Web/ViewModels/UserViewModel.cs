using DataDock.Common.Models;
using DataDock.Web.Models;
using Newtonsoft.Json;
using Octokit;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace DataDock.Web.ViewModels
{
    public class UserViewModel : BaseLayoutViewModel
    {
        public string GitHubAvatar { get; set; }

        public string GitHubLogin { get; set; }

        public string GitHubName { get; set; }

        public string GitHubUrl { get; set; }

        public IReadOnlyList<Repository> Repositories { get; set; }

        public OwnerInfo UserOwner { get; set; }

        public List<OwnerInfo> Organisations { get; set; }
        
        public UserViewModel()
        {
            this.Organisations = new List<OwnerInfo>();
        }

        public void Populate(ClaimsIdentity identity)
        {
            if (identity.IsAuthenticated)
            {
                GitHubName = identity.FindFirst(c => c.Type == ClaimTypes.Name)?.Value;
                GitHubLogin = identity.FindFirst(c => c.Type == DataDockClaimTypes.GitHubLogin)?.Value;
                GitHubUrl = identity.FindFirst(c => c.Type == DataDockClaimTypes.GitHubUrl)?.Value;
                GitHubAvatar = identity.FindFirst(c => c.Type == DataDockClaimTypes.GitHubAvatar)?.Value;

                UserOwner = new OwnerInfo
                {
                    OwnerId = GitHubLogin,
                    AvatarUrl = GitHubAvatar
                };

                foreach (var orgClaim in identity.Claims.Where(c =>
                    c.Type.Equals((DataDockClaimTypes.GitHubUserOrganization))))
                {
                    Organisations.Add(JsonConvert.DeserializeObject<OwnerInfo>(orgClaim.Value));
                }
                
            }
        }
    }
}
