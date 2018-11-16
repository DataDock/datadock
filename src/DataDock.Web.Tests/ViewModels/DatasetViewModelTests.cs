using System;
using System.Collections.Generic;
using System.Text;
using DataDock.Common;
using DataDock.Common.Models;
using DataDock.Web.ViewModels;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DataDock.Web.Tests.ViewModels
{
    public class DatasetViewModelTests
    {
        [Theory]
        [InlineData(@"{'dc:title': [{'@value':'a title'}, {'@value':'FR title', '@language':'fr'}]}", "en", "a title")]
        [InlineData(@"{'dc:title': [{'@value':'FR title', '@language':'fr'}, {'@value':'a title'}]}", "en", "a title")]
        [InlineData(@"{'dc:title': [{'@value':'a title'}, {'@value':'FR title', '@language':'fr'}]}", null, "a title")]
        [InlineData(@"{'dc:title': [{'@value':'FR title', '@language':'fr'}, {'@value':'a title'}]}", null, "FR title")]
        [InlineData(@"{'dc:title': [{'@value':'a title'}, {'@value':'FR title', '@language':'fr'}]}", "fr", "FR title")]
        [InlineData(@"{'dc:title': [{'@value':'FR title', '@language':'fr'}, 
                                      {'@value': 'an unscoped title'}, 
                                      {'@value':'a title', '@language': 'en'}]}",
            "en", "a title")]
        public void BestLanguageMatchReturnsUnscopedLiteralWhenNoMatch(string json, string prefLang, string expect)
        {
            var uriService = new DataDockUriService("http://datadock.io/");
            var testModel = new DatasetViewModel(uriService, new DatasetInfo{CsvwMetadata = JObject.Parse(json)}, prefLang);
            testModel.Title.Should().Be(expect);
        }
    }
}
