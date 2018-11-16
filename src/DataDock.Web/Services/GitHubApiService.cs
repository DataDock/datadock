using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using DataDock.Common;
using Octokit;
using Serilog;

namespace DataDock.Web.Services
{
    public class GitHubApiService : IGitHubApiService
    {
        private readonly IGitHubClientFactory _gitHubClientFactory;

        public GitHubApiService(IGitHubClientFactory gitHubClientFactory)
        {
            _gitHubClientFactory = gitHubClientFactory;
        }

        public async Task<List<string>> GetOwnerIdsForUserAsync(IIdentity identity)
        {
            if (identity == null) throw new ArgumentNullException();
            var ownerIdList = new List<string> { identity.Name };
            try
            {
                var ghClient = _gitHubClientFactory.CreateClient(identity as ClaimsIdentity);
                var orgs = await ghClient.Organization.GetAllForCurrent();
                if (orgs == null || !orgs.Any())
                {
                    Log.Warning("GetOwnerIdsForuserAsync: No organizations returned for user '{0}'", identity.Name);
                    return ownerIdList;
                }
                Log.Debug("GetOwnerIdsForuserAsync: {0} organizations found for user '{1}'", orgs.Count(), identity.Name);
                ownerIdList.AddRange(orgs.Select(o => o.Login));
                return ownerIdList;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetOwnerIdsForuserAsync: Error retrieving organization list for user {0}.", identity.Name);
                Log.Error(ex.ToString());
                return null;
            }
        }
        
        public async Task<List<Organization>> GetOrganizationsForUserAsync(IIdentity identity)
        {
            if (identity == null) throw new ArgumentNullException();
            var orgList = new List<Organization>();
            try
            {
                if (identity is ClaimsIdentity claimsIdentity)
                {
                    var ghClient = _gitHubClientFactory.CreateClient(claimsIdentity);

                    var orgs = await ghClient.Organization.GetAllForCurrent();
                    if (orgs == null || !orgs.Any())
                    {
                        Log.Warning("GetOrganizationsForUserAsync: No organizations returned for user '{0}'", identity.Name);
                        return orgList;
                    }
                    Log.Debug("GetOrganizationsForUserAsync: {0} organizations found for user '{1}'", orgs.Count(), identity.Name);

                    orgList = orgs.ToList();
                    return orgList;
                }

                Log.Error("Null reference when casting Identity to ClaimsIdentity for use in GitHub API");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetOrganizationsForUserAsync: Error retrieving organization list for user {0}.", identity.Name);
                Log.Error(ex.ToString());
                return null;
            }
        }

        public async Task<bool> UserIsAuthorizedForOrganization(IIdentity identity, string ownerId)
        {
            if (identity == null) throw new ArgumentNullException();
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentException("ownerId parameter is null or empty");
            try
            {
                var userOrgs = await GetOrganizationsForUserAsync(identity);
                var org = userOrgs.FirstOrDefault(o => o.Login == ownerId);
                return org != null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UserIsAuthorizedForOrganization: Error retrieving organization {0} for user {1}.", ownerId, identity.Name);
                Log.Error(ex.ToString());
                return false;
            }
        }

        public async Task<List<Repository>> GetRepositoryListForOwnerAsync(IIdentity identity, string ownerId)
        {
            if (identity == null) throw new ArgumentNullException();
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentException("ownerId parameter is null or empty");
            
            try
            {
                var ghClient = _gitHubClientFactory.CreateClient(identity as ClaimsIdentity);

                bool ownerIsUser = ownerId.Equals(identity.Name, StringComparison.InvariantCultureIgnoreCase);
                if (ownerIsUser)
                {
                    var repositories = await ghClient.Repository.GetAllForUser(ownerId);
                    if (repositories == null || !repositories.Any())
                    {
                        Log.Warning("GetRepositoryListForOwnerAsync: No repositories returned for ownerId '{0}'", ownerId);
                        return new List<Repository>();
                    }
                    Log.Debug("GetRepositoryListForOwnerAsync: {0} repositories found for ownerId '{1}'", repositories.Count(), ownerId);

                    return repositories.ToList();
                }
                else
                {
                    // owner is an organization
                    var repositories = await ghClient.Repository.GetAllForOrg(ownerId);
                    if (repositories == null || !repositories.Any())
                    {
                        Log.Warning("GetRepositoryListForOwnerAsync: No repositories returned for ownerId '{0}'",
                            ownerId);
                        return new List<Repository>();
                    }
                    Log.Debug("GetRepositoryListForOwnerAsync: {0} repositories found for ownerId '{1}'",
                        repositories.Count(), ownerId);

                    return repositories.ToList();
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetRepositoryListForOwnerAsync: Error retrieving repository list for ownerId {0}.", ownerId);
                Log.Error(ex.ToString());
                throw;
            }
        }
        
        public async Task<Repository> GetRepositoryAsync(IIdentity identity, string ownerId, string repoId)
        {
            if (identity == null) throw new ArgumentNullException();
            var repos = await GetRepositoryListForOwnerAsync(identity, ownerId);
            return repos.FirstOrDefault(r => r.Name.Equals(repoId, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
