using DataDock.Common.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DataDock.Web.ViewModels
{
    public class SettingsViewModel : DashboardViewModel
    {
        public string OwnerId { get; set; }
        public bool OwnerIsOrg { get; set; }

        /*
         * Default Publisher
         */
        [Display(Name = "Publisher Type", GroupName = "Publisher", Order = 0)]
        public string DefaultPublisherType { get; set; }

        [Display(Name = "Publisher Name", GroupName = "Publisher", Order = 1)]
        public string DefaultPublisherName { get; set; }

        [Display(Name = "Publisher Email", GroupName = "Publisher", Order = 2)]
        [DataType(DataType.EmailAddress, ErrorMessage = "E-mail is not valid")]
        public string DefaultPublisherEmail { get; set; }

        [Display(Name = "Publisher Website", GroupName = "Publisher", Order = 3)]
        [DataType(DataType.Url, ErrorMessage = "Website address is not valid")]
        public string DefaultPublisherWebsite { get; set; }

        public IEnumerable<SelectListItem> PublisherTypes { get; set; }
        /*
         * Portal Settings
         */

        [Display(Name = "Portal Search Buttons")]
        public string SearchButtons { get; set; }


        /*
         * Last Modified
         */
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; }

        public SettingsViewModel()
        {
            this.PublisherTypes = new List<SelectListItem>
            {
                new SelectListItem { Text = "Person", Value = "http://xmlns.com/foaf/0.1/Person" },
                new SelectListItem { Text = "Organization", Value = "http://xmlns.com/foaf/0.1/Organization" }
            };
        }
    }
}
