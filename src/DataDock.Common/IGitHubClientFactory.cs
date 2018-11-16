using System.Security.Claims;
using Octokit;

namespace DataDock.Common
{
    public interface IGitHubClientFactory
    {
        IGitHubClient CreateClient(ClaimsIdentity identity);

        GitHubClient CreateClient(string accessToken);

    }

}
