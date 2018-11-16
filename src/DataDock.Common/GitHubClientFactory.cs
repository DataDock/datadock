using System;
using System.Linq;
using System.Security.Claims;
using DataDock.Common.Models;
using Octokit;
using Serilog;

namespace DataDock.Common
{
    public class GitHubClientFactory : IGitHubClientFactory
    {
        private readonly string _productHeaderValue;
        public GitHubClientFactory(string productHeaderValue)
        {
            _productHeaderValue = productHeaderValue;
        }

        public IGitHubClient CreateClient(ClaimsIdentity identity)
        {
            try
            {
                Log.Debug("CreateClient for app '{0}'", _productHeaderValue);

                if (identity == null)
                {
                    Log.Error("Unable to initialise GitHubClient, no user identity supplied.");
                    return null;
                }
                var claims = identity.Claims;
                var accessTokenClaim = claims.FirstOrDefault(x => x.Type == DataDockClaimTypes.GitHubAccessToken);
                var accessToken = accessTokenClaim?.Value;
                if (string.IsNullOrEmpty(accessToken))
                {
                    Log.Error("Unable to initialise GitHubClient, no access token found.");
                    return null;
                }

                return CreateClient(accessToken);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error creating GitHubClient");
                throw;
            }
        }

        public GitHubClient CreateClient(string accessToken)
        {
            if (accessToken == null) throw new ArgumentNullException(nameof(accessToken));
            var client = new GitHubClient(new ProductHeaderValue(_productHeaderValue))
            {
                Credentials = new Credentials(accessToken)
            };
            return client;
        }


    }
}