using System;
using Nest;

namespace DataDock.Common.Models
{
    [ElasticsearchType(Name="usersettings", IdProperty = "UserId")]
    public class UserSettings
    {
        /// <summary>
        /// The DataDock User ID
        /// </summary>
        [Keyword]
        public string UserId { get; set; }
        
        /// <summary>
        /// Date and time that the settings were last changed
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// UserId of the person who last changed the settings
        /// </summary>
        [Keyword]
        public string LastModifiedBy { get; set; }
    }
}
