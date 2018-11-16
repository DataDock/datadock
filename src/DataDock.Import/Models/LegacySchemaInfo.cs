using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace DataDock.Import.Models
{
    internal class LegacySchemaInfo
    {
        public LegacySchemaInfo()
        {
            this.Type = "schema";
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string Type { get; set; }

        public string OwnerId { get; set; }

        public string RepositoryId { get; set; }

        public string SchemaId { get; set; }

        public DateTime LastModified { get; set; }

        /// <summary>
        /// JSON of schema 
        /// e.g. { dc:title "schema title", metadata: { metadataJson } }
        /// </summary>
        public dynamic Schema { get; set; }
    }
}
