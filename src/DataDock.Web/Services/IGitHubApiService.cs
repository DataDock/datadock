using Octokit;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace DataDock.Web.Services
{
    public interface IGitHubApiService
    {
        Task<List<string>> GetOwnerIdsForUserAsync(IIdentity identity);

        Task<List<Organization>> GetOrganizationsForUserAsync(IIdentity identity);

        Task<bool> UserIsAuthorizedForOrganization(IIdentity identity, string ownerId);

        Task<List<Repository>> GetRepositoryListForOwnerAsync(IIdentity identity, string ownerId);

        Task<Repository> GetRepositoryAsync(IIdentity identity, string ownerId, string repoId);

    }
}
