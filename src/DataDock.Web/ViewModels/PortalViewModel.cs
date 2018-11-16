using DataDock.Common.Models;
using DataDock.Web.Models;
using Serilog;
using System;
using System.Collections.Generic;

namespace DataDock.Web.ViewModels
{
    public class PortalViewModel
    {
        public string DataDockBaseUrl { get; set; }

        public string OwnerId { get; set; }

        public bool IsOrg { get; set; }

        /// <summary>
        /// The owner GitHub avatar URL 
        /// </summary>
        /// <remarks>Empty if the owner settings display = false</remarks>
        public string LogoUrl { get; set; }

        /// <summary>
        /// The owner GitHub description ("Bio" in GitHub)
        /// </summary>
        /// <remarks>Empty if the owner settings display = false</remarks>
        public string Description { get; set; }

        public string OwnerDisplayName { get; set; }

        public string Location { get; set; }

        public string Website { get; set; }

        public string Twitter { get; set; }

        public string GitHubHtmlUrl { get; set; }

        public bool ShowAvatar { get; set; }

        public bool ShowLocation { get; set; }

        public bool ShowWebsiteLink { get; set; }

        public bool ShowDescription { get; set; }

        public bool ShowDashboardLink { get; set; }

        public bool ShowIssuesLink { get; set; }

        public ContactInfo Publisher { get; set; }

        public IList<SearchButton> OwnerSearchButtons { get; set; }

        public IList<string> RepoIds { get; set; }

        public PortalViewModel() { }

        public PortalViewModel(OwnerSettings ownerSettings)
        {
            this.OwnerId = ownerSettings.OwnerId;
           
            this.IsOrg = ownerSettings.IsOrg;
            this.Twitter = ownerSettings.TwitterHandle;
            this.ShowDashboardLink = ownerSettings.DisplayDataDockLink;
            this.ShowAvatar = ownerSettings.DisplayGitHubAvatar;
            this.ShowLocation = ownerSettings.DisplayGitHubLocation;
            this.ShowWebsiteLink = ownerSettings.DisplayGitHubBlogUrl;
            this.ShowIssuesLink = ownerSettings.DisplayGitHubIssuesLink;
            this.ShowDescription = ownerSettings.DisplayGitHubDescription;

            this.Publisher = ownerSettings.DefaultPublisher;

            this.OwnerSearchButtons = this.GetSearchButtons(ownerSettings.SearchButtons);

            Log.Debug("Using GitHubClient to retrieve up to date github account info for display on the portal homepage");

            this.OwnerDisplayName = this.OwnerId;
        }

        private List<SearchButton> GetSearchButtons(string searchButtonsString)
        {
            try
            {
                var searchButtons = new List<SearchButton>();

                var sbSplit = searchButtonsString.Split(',');
                foreach (var b in sbSplit)
                {
                    var sb = new SearchButton();
                    if (b.IndexOf(';') >= 0)
                    {
                        // has different button text
                        var bSplit = b.Split(';');
                        sb.Tag = bSplit[0];
                        sb.Text = bSplit[1];
                        if (bSplit.Length > 2) sb.Icon = bSplit[2];
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
            catch (Exception e)
            {
               Log.Error(e, "Error building search buttons from string saved to owner settings");
            }
            return new List<SearchButton>();
        }
    }

    public class SearchButton
    {
        public string Tag { get; set; }

        public string Text { get; set; }

        public string Icon { get; set; }
    }
}
