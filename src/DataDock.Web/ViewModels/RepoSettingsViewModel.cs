using DataDock.Common.Models;

namespace DataDock.Web.ViewModels
{
    public class RepoSettingsViewModel : SettingsViewModel
    {
        public string RepoId { get; set; }

        public RepoSettingsViewModel()
        {
        }
        
        public RepoSettingsViewModel(RepoSettings repoSettings)
        {
            OwnerId = repoSettings.OwnerId;
            RepoId = repoSettings.RepositoryId;
            OwnerIsOrg = repoSettings.OwnerIsOrg;
            DefaultPublisherName = repoSettings.DefaultPublisher?.Label;
            DefaultPublisherType = repoSettings.DefaultPublisher?.Type;
            DefaultPublisherEmail = repoSettings.DefaultPublisher?.Email;
            DefaultPublisherWebsite = repoSettings.DefaultPublisher?.Website;
            SearchButtons = repoSettings.SearchButtons;
            LastModifiedBy = repoSettings.LastModifiedBy;
            LastModified = repoSettings.LastModified;
        }

        public RepoSettings AsRepoSettings()
        {
            return new RepoSettings()
            {
                OwnerId = this.OwnerId,
                RepositoryId = this.RepoId,
                OwnerIsOrg = this.OwnerIsOrg,
                DefaultPublisher = new ContactInfo
                {
                    Label = this.DefaultPublisherName,
                    Type = this.DefaultPublisherType,
                    Email = this.DefaultPublisherEmail,
                    Website = this.DefaultPublisherWebsite
                },
                SearchButtons = this.SearchButtons,
                LastModifiedBy = this.LastModifiedBy,
                LastModified = this.LastModified
            };
        }
    }
}
