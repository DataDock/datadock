using DataDock.Common.Models;
using DataDock.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DataDock.Web.ViewModels
{
    public class DatasetViewModel
    {
        private readonly IDataDockUriService _uriService;
        private readonly DatasetInfo _datasetInfo;
        private readonly JObject _csvwMetadata;
        private readonly JObject _voidMetadata;
        private readonly string _prefLang;

        public DatasetViewModel(IDataDockUriService uriService, DatasetInfo datasetInfo, string prefLang=null, bool isOwner = false)
        {
            _uriService = uriService;
            _datasetInfo = datasetInfo;
            _csvwMetadata = datasetInfo.CsvwMetadata as JObject;
            _voidMetadata = datasetInfo.VoidMetadata as JObject;
            _prefLang = prefLang;

            Title = this.GetTitle();
            Description = this.GetDescription();
            IsOwner = isOwner;
        }

        [Display(Name = "Identifier")]
        public string Id => _datasetInfo.DatasetId;
        public string RepositoryId => _datasetInfo.RepositoryId;
        public string OwnerId => _datasetInfo.OwnerId;

        public string Iri => this.GetIri();

        [Display(Name = "Title")]
        public string Title { get; set; }
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Tags")]
        public IEnumerable<string> Tags => this.GetTags();

        [Display(Name = "License")]
        public string LicenseUri => this.GetLicenseUri();
        public string LicenseIcon => this.GetLicenseIcon();

        [Display(Name = "Last Modified")]
        public DateTime LastModified => _datasetInfo.LastModified;

        public bool? ShowOnHomePage => _datasetInfo.ShowOnHomePage;

        public bool IsOwner { get; set; }

        public string GetIri()
        {
            if (_csvwMetadata != null)
            {
                return _csvwMetadata["url"].ToString();
            }
            return _uriService.GetDatasetIdentifier(_datasetInfo.OwnerId, _datasetInfo.RepositoryId, _datasetInfo.DatasetId);
        }

        public string GetTitle()
        {
            if (_csvwMetadata != null)
            {
                return GetLiteralValue(_csvwMetadata, "dc:title");
            }
            return _datasetInfo.RepositoryId + "/" + _datasetInfo.DatasetId;
        }

        public string GetDescription()
        {
            if (_csvwMetadata != null)
            {
                return GetLiteralValue(_csvwMetadata, "dc:description");
            }
            return string.Empty;
        }

        public string GetLicenseUri()
        {
            if (_csvwMetadata != null)
            {
                return GetLiteralValue(_csvwMetadata, "dc:license");
            }
            return string.Empty;
        }

        public string GetLicenseIcon()
        {
            switch (GetLicenseUri())
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

        public IEnumerable<string> GetTags()
        {
            if (_csvwMetadata?["dcat:keyword"] is JArray tags)
            {
                return tags.Select(t => (t as JValue)?.Value<string>());
            }
            return new string[0];
        }


        private string GetLiteralValue(JObject parentObject, string propertyName, string defaultValue = null)
        {
            switch (parentObject[propertyName])
            {
                case JArray titles:
                    return GetBestLanguageMatch(titles);
                case JValue title:
                    return GetLiteralValue(title);
            }

            return defaultValue;
        }

        private static string GetLiteralValue(JToken literalToken)
        {
            if (literalToken is JObject litObj) return (litObj["@value"] as JValue)?.Value<string>();
            return (literalToken as JValue)?.Value<string>();
        }

        private string GetBestLanguageMatch(JArray literalArray)
        {
            if (_prefLang == null)
            {
                // Just the first object with an @value property
                return literalArray
                    .Cast<JObject>()
                    .Select(o => o["@value"] as JValue)
                    .FirstOrDefault()
                    ?.Value<string>();
            }

            JObject bestMatch = null;
            var bestMatchCount = -2;
            var prefToks = _prefLang.Split('-');
            foreach (var tok in literalArray)
            {
                if (!(tok is JObject litObj)) continue;
                var lang = litObj["@language"]?.Value<string>();
                var matchCount = 0;
                if (lang != null)
                {
                    var langToks = lang.Split('-');
                    for (var i = 0; i < Math.Min(langToks.Length, prefToks.Length); i++)
                    {
                        if (langToks[i].Equals(prefToks[i], StringComparison.InvariantCultureIgnoreCase))
                        {
                            matchCount+=2;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    // With no language code, count as a simple match
                    matchCount = 1;
                }

                if (matchCount > bestMatchCount)
                {
                    bestMatchCount = matchCount;
                    bestMatch = litObj;
                    if (bestMatchCount == (prefToks.Length*2))
                    {
                        // Exit early on a perfect match
                        break;
                    }
                }
            }
            return bestMatch?["@value"]?.Value<string>();
        }
    }
}
