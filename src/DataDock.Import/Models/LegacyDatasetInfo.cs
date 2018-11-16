using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace DataDock.Import.Models
{
    internal class LegacyDatasetInfo
    {
        public LegacyDatasetInfo()
        {
            this.Type = "dataset";
        }


        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string Type { get; set; }

        public string OwnerId { get; set; }

        public string RepositoryId { get; set; }

        public string DatasetId { get; set; }

        public DateTime LastModified { get; set; }

        /// <summary>
        /// CSVW Metadata
        /// </summary>
        public dynamic Metadata { get; set; }

        /// <summary>
        /// VoID Metadata
        /// </summary>
        public dynamic VoidMetadata { get; set; }

        public bool? ShowOnHomePage { get; set; }
    }
}
