using System;
using Nest;

namespace DataDock.Common.Models
{
    [ElasticsearchType(Name = "ownersettings", IdProperty = "OwnerId")]
    public class OwnerSettings
    {
        [Keyword]
        public string OwnerId { get; set; }

        [Boolean]
        public bool IsOrg { get; set; }

        /// <summary>
        /// The default publisher info to use on all repositories under this owner 
        /// (unless overwritten by a publisher setting at the repository level 
        /// or on a single dataset upload
        /// </summary>
        public ContactInfo DefaultPublisher { get; set; }

        /// <summary>
        /// Date and time that the settings were last changed
        /// </summary>
        [Date]
        public DateTime LastModified { get; set; }

        /// <summary>
        /// UserId of the person who last changed the settings
        /// </summary>
        [Keyword]
        public string LastModifiedBy { get; set; }

        [Boolean]
        public bool DisplayGitHubIssuesLink { get; set; }

        [Keyword]
        public string TwitterHandle { get; set; }

        [Keyword]
        public string LinkedInProfileUrl { get; set; }

        [Boolean]
        public bool DisplayGitHubAvatar { get; set; }

        [Boolean]
        public bool DisplayGitHubDescription { get; set; }

        [Boolean]
        public bool DisplayDataDockLink { get; set; }

        [Boolean]
        public bool DisplayGitHubLocation { get; set; }

        [Boolean]
        public bool DisplayGitHubBlogUrl { get; set; }

        [Text]
        public string SearchButtons { get; set; }

    }
}
