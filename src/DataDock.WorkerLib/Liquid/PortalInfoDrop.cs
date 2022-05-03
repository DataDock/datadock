using System.Collections.Generic;
using DotLiquid;

namespace DataDock.Worker.Liquid
{
    public class PortalInfoDrop : Drop
    {
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

        public string RepositoryName { get; set; }

        public bool ShowDashboardLink { get; set; }

        public bool ShowIssuesLink { get; set; }

        public IList<SearchButtonDrop> RepoSearchButtons { get; set; }
    }

    public class SearchButtonDrop : Drop
    {
        public string Tag { get; set; }

        public string Text { get; set; }
    }
}
