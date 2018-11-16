using System;
using Nest;

namespace DataDock.Common.Models
{
    [ElasticsearchType(Name = "reposettings", IdProperty = "Id")]
    public class RepoSettings
    {
        /// <summary>
        /// Combined owner and repo IDs in the format {ownerId}/{repositoryId}
        /// </summary>
        [Keyword]
        public string Id { get; set; }

        /// <summary>
        /// The repository owner
        /// </summary>
        [Keyword]
        public string OwnerId { get; set; }

        /// <summary>
        /// The DataDock repository ID
        /// </summary>
        [Keyword]
        public string RepositoryId { get; set; }

        [Boolean]
        public bool OwnerIsOrg { get; set; }

        [Keyword]
        public string CloneUrl { get; set; }
        
        /// <summary>
        /// The link to the avatar image of the repository owner (may be null)
        /// </summary>
        [Text]
        public string OwnerAvatar { get; set; }

        /// <summary>
        /// The publisher metadata for this repository
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

        [Text]
        public string SearchButtons { get; set; }

    }
}
