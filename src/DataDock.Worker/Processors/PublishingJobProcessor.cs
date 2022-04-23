using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataDock.Common;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Worker.Liquid;
using Serilog;

namespace DataDock.Worker.Processors
{
    public abstract class PublishingJobProcessor : IDataDockProcessor
    {
        public WorkerConfiguration Configuration { get; }
        public IProgressLog ProgressLog { get; private set; }
        public JobInfo JobInfo { get; private set; }
        protected string AuthenticationToken { get; private set; }
        protected IOwnerSettingsStore OwnerSettingsStore { get; private set; }
        protected IRepoSettingsStore RepoSettingsStore { get; private set; }
        protected IGitHubClientFactory GitHubClientFactory { get; private set; }

        protected PublishingJobProcessor(WorkerConfiguration configuration, 
            IOwnerSettingsStore ownerSettingsStore,
            IRepoSettingsStore repoSettingsStore, 
            IGitHubClientFactory gitHubClientFactory)
        {
            Configuration = configuration;
            OwnerSettingsStore = ownerSettingsStore;
            RepoSettingsStore = repoSettingsStore;
            GitHubClientFactory = gitHubClientFactory;
        }

        public async Task ProcessJob(JobInfo jobInfo, UserAccount userInfo, IProgressLog progressLog)
        {
            ProgressLog = progressLog;
            JobInfo = jobInfo;
            var authenticationClaim =
                userInfo.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.GitHubAccessToken));
            AuthenticationToken = authenticationClaim?.Value;
            if (string.IsNullOrEmpty(AuthenticationToken))
            {
                Log.Error("No authentication token found for user {userId}", userInfo.UserId);
                ProgressLog.Error("Could not find a valid GitHub access token for this user account. Please check your account settings.");
                throw new WorkerException("Could not find a valid GitHub access token for this user account. Please check your account settings.");
            }
            await RunJob(jobInfo, userInfo);
        }

        protected abstract Task RunJob(JobInfo jobInfo, UserAccount userInfo);

        public async Task UpdateHtmlPagesAsync(IDataDockRepository dataDockRepository, Uri[] graphFilter)
        {
            var portalInfo = await GetPortalSettingsInfo(JobInfo.OwnerId, JobInfo.RepositoryId, AuthenticationToken);
            //TODO get datadock-publish-url from config? page template are always remote as they are pushed to github
            var templateVariables =
                new Dictionary<string, object>
                {
                    {"datadock-publish-url", "https://datadock.io" },
                    {"owner-id", JobInfo.OwnerId},
                    {"repo-id", JobInfo.RepositoryId},
                    {"portal-info", portalInfo},
                };
            dataDockRepository.Publish(graphFilter, templateVariables);
        }

        private async Task<PortalInfoDrop> GetPortalSettingsInfo(string ownerId, string repoId, string authenticationToken)
        {
            try
            {
                ProgressLog.Info("Attempting to retrieve portal settings information from owner settings");
                if (ownerId != null)
                {
                    var portalInfo = new PortalInfoDrop
                    {
                        OwnerId = ownerId,
                        RepositoryName = repoId
                    };

                    ProgressLog.Info("Attempting to retrieve publisher contact information from repository owner's settings");
                    var ownerSettings = await OwnerSettingsStore.GetOwnerSettingsAsync(ownerId);
                    if (ownerSettings != null)
                    {
                        ProgressLog.Info($"No contact information found in repository owner's settings. Attempting to retrieve contact information from GitHub.");
                        portalInfo.IsOrg = ownerSettings.IsOrg;
                        portalInfo.ShowDashboardLink = ownerSettings.DisplayDataDockLink;
                        if (!string.IsNullOrEmpty(ownerSettings.TwitterHandle)) portalInfo.Twitter = ownerSettings.TwitterHandle;

                        var client = GitHubClientFactory.CreateClient(authenticationToken);
                        if (ownerSettings.IsOrg)
                        {
                            ProgressLog.Info("Attempting to retrieve contact information for GitHub organization {ownerId}.");
                            var org = await client.Organization.Get(ownerId);
                            if (org == null) return portalInfo;

                            portalInfo.OwnerDisplayName = org.Name ?? ownerId;
                            if (ownerSettings.DisplayGitHubBlogUrl) portalInfo.Website = org.Blog;
                            if (ownerSettings.DisplayGitHubAvatar) portalInfo.LogoUrl = org.AvatarUrl;
                            if (ownerSettings.DisplayGitHubDescription) portalInfo.Description = org.Bio;
                            if (ownerSettings.DisplayGitHubBlogUrl) portalInfo.Website = org.Blog;
                            if (ownerSettings.DisplayGitHubLocation) portalInfo.Location = org.Location;
                            if (ownerSettings.DisplayGitHubIssuesLink) portalInfo.GitHubHtmlUrl = org.HtmlUrl;
                        }
                        else
                        {
                            ProgressLog.Info("Attempting to retrieve contact information for GitHub user {ownerId}.");
                            var user = await client.User.Get(ownerId);
                            if (user == null) return portalInfo;

                            portalInfo.OwnerDisplayName = user.Name ?? ownerId;
                            if (ownerSettings.DisplayGitHubBlogUrl) portalInfo.Website = user.Blog;
                            if (ownerSettings.DisplayGitHubAvatar) portalInfo.LogoUrl = user.AvatarUrl;
                            if (ownerSettings.DisplayGitHubDescription) portalInfo.Description = user.Bio;
                            if (ownerSettings.DisplayGitHubBlogUrl) portalInfo.Website = user.Blog;
                            if (ownerSettings.DisplayGitHubLocation) portalInfo.Location = user.Location;
                            if (ownerSettings.DisplayGitHubIssuesLink) portalInfo.GitHubHtmlUrl = user.HtmlUrl;
                        }
                    }
                    ProgressLog.Info("Looking up repository portal search buttons from settings for {0} repository.", repoId);

                    var repoSettings = await RepoSettingsStore.GetRepoSettingsAsync(ownerId, repoId);
                    var repoSearchButtons = repoSettings?.SearchButtons;
                    if (!string.IsNullOrEmpty(repoSearchButtons))
                    {

                        portalInfo.RepoSearchButtons = GetSearchButtons(repoSearchButtons);

                    }
                    return portalInfo;
                }
                // no settings 
                ProgressLog.Info("No owner settings found");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when attempting to retrieve portal information from owner settings.");
                ProgressLog.Error("Error when attempting to retrieve portal information from owner settings");
                return null;
            }

        }

        private List<SearchButtonDrop> GetSearchButtons(string searchButtonsString)
        {
            var sbSplit = searchButtonsString.Split(',');
            var searchButtons = new List<SearchButtonDrop>();
            foreach (var b in sbSplit)
            {
                var sb = new SearchButtonDrop();
                if (b.IndexOf(';') >= 0)
                {
                    // has different button text
                    var bSplit = b.Split(';');
                    sb.Tag = bSplit[0];
                    sb.Text = bSplit[1];
                    searchButtons.Add(sb);
                }
                else
                {
                    sb.Tag = b;
                    searchButtons.Add(sb);
                }
            }
            return searchButtons;
        }


    }
}
