using DataDock.Common.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Serilog;

namespace DataDock.Web.ViewModels
{
    public class TemplateViewModel
    {
        private readonly SchemaInfo _schemaInfo;
        private readonly JObject _schema;
        private readonly JObject _metadata;

        public TemplateViewModel(SchemaInfo schemaInfo)
        {
            try
            {
                if (schemaInfo == null) return;
                _schemaInfo = schemaInfo;
                _schema = _schemaInfo.Schema;
                Title = this.GetTitle();
                if (_schema == null) return;

                _metadata = _schema["metadata"] as JObject;
                // set basic metadata properties
                if (_metadata != null)
                {
                    MetadataRaw = _metadata.ToString();
                    MetadataTitle = GetLiteralValue(_metadata, "dc:title");
                    MetadataDescription = GetLiteralValue(_metadata, "dc:description");
                    MetadataTags = GetMetadataTags();
                    MetadataLicenseUri = GetLicenseUri(_metadata);
                    MetadataLicenseIcon = GetLicenseIcon(_metadata);
                }
               
            }
            catch (Exception e)
            {
                Log.Error(e, "Error converting schemaInfo into TemplateViewModel");
                throw;
            }
            
        }

        public string Id => _schemaInfo?.Id;
        public string OwnerId => _schemaInfo?.OwnerId;
        public string RepositoryId => _schemaInfo?.RepositoryId;
        public string SchemaId => _schemaInfo?.SchemaId;
        
        
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Display(Name = "Last Modified")]
        public DateTime LastModified => _schemaInfo?.LastModified ?? DateTime.MinValue;

        public string MetadataTitle { get; set; }
        public string MetadataDescription { get; set; }
        public IEnumerable<string> MetadataTags { get; set; }
        public string MetadataLicenseUri { get; set; }
        public string MetadataLicenseIcon { get; set; }
        public string MetadataRaw { get; set; }

        public string GetTitle()
        {
            if (_schema != null)
            {
                return GetLiteralValue(_schema, "dc:title");
            }

            if (_schemaInfo != null)
            {
                return _schemaInfo.RepositoryId + "/" + _schemaInfo.SchemaId;
            }

            return string.Empty;
        }

        public string GetDescription()
        {
            if (_schema != null)
            {
                return GetLiteralValue(_schema, "dc:description");
            }
            return string.Empty;
        }
        
        public IEnumerable<string> GetMetadataTags()
        {
            if(_metadata == null) return new List<string>();
            var tags = _metadata?["dcat:keyword"] as JArray;
            if (tags != null)
            {
                return tags.Select(t => (t as JValue)?.Value<string>());
            }
            return new string[0];
        }

        public string GetLicenseUri(JObject parentObject)
        {
            if (parentObject != null)
            {
                return GetLiteralValue(parentObject, "dc:license");
            }
            return string.Empty;
        }

        public string GetLicenseIcon(JObject parentObject)
        {
            if (parentObject == null) return string.Empty;
            switch (GetLicenseUri(parentObject))
            {
                case "https://creativecommons.org/publicdomain/zero/1.0/":
                    return "cc-zero.png";
                case "https://creativecommons.org/licenses/by/4.0/":
                    return "cc-by.png";
                case "https://creativecommons.org/licenses/by-sa/4.0/":
                    return "cc-by-sa.png";
                case "http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/":
                    return "ogl.png";
                case "https://opendatacommons.org/licenses/pddl/":
                    return "PDDL.png";
                case "https://opendatacommons.org/licenses/by/":
                    return "ODC-By.png";
            }
            return string.Empty;
        }

        private static string GetLiteralValue(JObject parentObject, string propertyName, string defaultValue = null)
        {
            if (parentObject == null) return defaultValue;
            var titles = parentObject[propertyName] as JArray;
            if (titles != null) return GetBestLanguageMatch(titles, null);
            var title = parentObject[propertyName] as JValue;
            if (title != null) return GetLiteralValue(title);
            return defaultValue;
        }

        private static string GetLiteralValue(JToken literalToken)
        {
            var litObj = literalToken as JObject;
            if (litObj != null) return (litObj["@value"] as JValue)?.Value<string>();
            return (literalToken as JValue)?.Value<string>();
        }

        private static string GetBestLanguageMatch(JArray literalArray, string prefLang)
        {
            // TODO: Language matching. Currently this returns the first literal in the array (issue #86)
            var match = literalArray[0];
            return GetLiteralValue(match);
        }
    }
}
