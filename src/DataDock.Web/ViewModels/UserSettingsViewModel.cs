using DataDock.Common.Models;
using Microsoft.AspNetCore.Mvc;
using System;

namespace DataDock.Web.ViewModels
{
    public class UserSettingsViewModel : DashboardViewModel
    {
        [HiddenInput]
        public string UserId { get; set; }
        
        /*
        * Last Modified
        */
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; }

        public UserSettingsViewModel()
        {
        }
        public UserSettingsViewModel(UserSettings settings)
        {
            UserId = settings.UserId;
            LastModified = settings.LastModified;
            LastModifiedBy = settings.LastModifiedBy;
        }

        public UserSettings AsUserSettings()
        {
            return new UserSettings
            {
                UserId = this.UserId,
                LastModified = this.LastModified,
                LastModifiedBy = this.LastModifiedBy
            };

        }
    }

   
    
}
