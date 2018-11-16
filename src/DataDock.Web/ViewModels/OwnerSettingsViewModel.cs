using DataDock.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace DataDock.Web.ViewModels
{
    public class OwnerSettingsViewModel  : SettingsViewModel
    {
        [Display(Name = "Twitter Handle")]
        public string TwitterHandle { get; set; }

        [Display(Name = "LinkedIn Profile URL")]
        [DataType(DataType.Url, ErrorMessage = "Web address is not valid")]
        public string LinkedInProfileUrl { get; set; }


        [Display(Name = "Display Link To DataDock Management Dashboard")]
        public bool DisplayDataDockLink { get; set; }

        [Display(Name = "Display Link To GitHub Issues List")]
        public bool DisplayGitHubIssuesLink { get; set; }

        [Display(Name = "Display GitHub Avatar")]
        public bool DisplayGitHubAvatar { get; set; }

        [Display(Name = "Display Description (as set as GitHub 'Bio')")]
        public bool DisplayGitHubDescription { get; set; }

        [Display(Name = "Display Website URL (as set in GitHub Profile)")]
        public bool DisplayGitHubBlogUrl { get; set; }

        [Display(Name = "Display Location (as set in GitHub Profile)")]
        public bool DisplayGitHubLocation { get; set; }

       
        public OwnerSettingsViewModel() { }

        public OwnerSettingsViewModel(OwnerSettings ownerSettings)
        {
            OwnerId = ownerSettings.OwnerId;
            DefaultPublisherName = ownerSettings.DefaultPublisher?.Label;
            DefaultPublisherType = ownerSettings.DefaultPublisher?.Type;
            DefaultPublisherEmail = ownerSettings.DefaultPublisher?.Email;
            DefaultPublisherWebsite = ownerSettings.DefaultPublisher?.Website;
            TwitterHandle = ownerSettings.TwitterHandle;
            LinkedInProfileUrl = ownerSettings.LinkedInProfileUrl;
            DisplayDataDockLink = ownerSettings.DisplayDataDockLink;
            DisplayGitHubIssuesLink = ownerSettings.DisplayGitHubIssuesLink;
            DisplayGitHubAvatar = ownerSettings.DisplayGitHubAvatar;
            DisplayGitHubDescription = ownerSettings.DisplayGitHubDescription;
            DisplayGitHubLocation = ownerSettings.DisplayGitHubLocation;
            DisplayGitHubBlogUrl = ownerSettings.DisplayGitHubBlogUrl;
            SearchButtons = ownerSettings.SearchButtons;
            LastModifiedBy = ownerSettings.LastModifiedBy;
            LastModified = ownerSettings.LastModified;
        }

        public OwnerSettings AsOwnerSettings()
        {
            return new OwnerSettings()
            {
                OwnerId = this.OwnerId,
                DefaultPublisher = new ContactInfo
                {
                    Label = this.DefaultPublisherName,
                    Type = this.DefaultPublisherType,
                    Email = this.DefaultPublisherEmail,
                    Website = this.DefaultPublisherWebsite
                },
                IsOrg = this.OwnerIsOrg,
                TwitterHandle = !string.IsNullOrEmpty(this.TwitterHandle) ? this.TwitterHandle.Replace("@", "").Trim() : null,
                LinkedInProfileUrl = this.LinkedInProfileUrl,
                DisplayDataDockLink = this.DisplayDataDockLink,
                DisplayGitHubIssuesLink = this.DisplayGitHubIssuesLink,
                DisplayGitHubAvatar = this.DisplayGitHubAvatar,
                DisplayGitHubDescription = this.DisplayGitHubDescription,
                DisplayGitHubLocation = this.DisplayGitHubLocation,
                DisplayGitHubBlogUrl = this.DisplayGitHubBlogUrl,
                SearchButtons = this.SearchButtons,
                LastModifiedBy = this.LastModifiedBy,
                LastModified = this.LastModified
            };
        }
    }
    
}
