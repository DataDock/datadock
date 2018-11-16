using System;
using System.Linq;
using DataDock.Worker;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class MetadataExtractorSpec
    {
        [Fact]
        public void ItAssertsRdfType()
        {
            var ex = new MetdataExtractor();
            var obj = new JObject(new JProperty("url", "http://datadock.io/test/repo/data"));
            var g = new Graph();
            ex.Run(obj, g, new Uri("http://datadock.io/test/repo/publisher"), 100, null);
            var expectSubject = g.CreateUriNode(new Uri("http://datadock.io/test/repo/data"));
            GraphShouldContainRdfTypeStatement(g, expectSubject);
        }

        [Fact]
        public void ItAssertsDcPublisher()
        {
            var ex = new MetdataExtractor();
            var obj = new JObject(new JProperty("url", "http://datadock.io/test/repo/data"));
            var g = new Graph();
            ex.Run(obj, g, new Uri("http://datadock.io/test/repo/publisher"), 100, null);
            var expectSubject = g.CreateUriNode(new Uri("http://datadock.io/test/repo/data"));
            var expectPublisher = g.CreateUriNode(new Uri("http://datadock.io/test/repo/publisher"));
            GraphShouldContainDcPublisherStatement(g, expectSubject, expectPublisher);
        }

        [Fact]
        public void ItAssertsVoidTriples()
        {
            var ex = new MetdataExtractor();
            var obj = new JObject(new JProperty("url", "http://datadock.io/test/repo/data"));
            var g = new Graph();
            ex.Run(obj, g, new Uri("http://datadock.io/test/repo/publisher"), 100, null);
            var expectSubject = g.CreateUriNode(new Uri("http://datadock.io/test/repo/data"));
            var voidTriples = g.CreateUriNode(new Uri("http://rdfs.org/ns/void#triples"));
            var expectTripleCount = g.CreateLiteralNode("100", new Uri("http://www.w3.org/2001/XMLSchema#integer"));
            var statements = g.GetTriplesWithSubjectPredicate(expectSubject, voidTriples);
            statements.Count().Should().Be(1);
            statements.Should().Contain(x => x.Object.Equals(expectTripleCount));
        }

        [Fact]
        public void ItCanProcessDcTitleStringField()
        {
            var ex = new MetdataExtractor();
            var obj = new JObject(new JProperty("url", "http://datadock.io/test/repo/data"),
                new JProperty("dc:title", "This is a title"));
            var g = new Graph();
            ex.Run(obj, g, new Uri("http://datadock.io/test/repo/publisher"), 100, null);

            var expectSubject = g.CreateUriNode(new Uri("http://datadock.io/test/repo/data"));
            var expectPredicate = g.CreateUriNode(new Uri("http://purl.org/dc/terms/title"));

            var titleStatement = g.GetTriplesWithSubjectPredicate(expectSubject, expectPredicate).FirstOrDefault();
            titleStatement.Should().NotBeNull();
            titleStatement.Object.Should().BeAssignableTo<ILiteralNode>().Which.Value.Should().Be("This is a title");
        }

        [Fact]
        public void ItCanProcessDcDescriptionStringField()
        {
            var ex = new MetdataExtractor();
            var obj = new JObject(new JProperty("url", "http://datadock.io/test/repo/data"),
                new JProperty("dc:description", "This is a description"));
            var g = new Graph();
            ex.Run(obj, g, new Uri("http://datadock.io/test/repo/publisher"), 100, null);

            var expectSubject = g.CreateUriNode(new Uri("http://datadock.io/test/repo/data"));
            var expectPredicate = g.CreateUriNode(new Uri("http://purl.org/dc/terms/description"));
            var statements = g.GetTriplesWithSubjectPredicate(expectSubject, expectPredicate).ToList();
            var titleStatement = statements[0];
            titleStatement.Object.Should()
                .BeAssignableTo<ILiteralNode>()
                .Which.Value.Should()
                .Be("This is a description");
        }

        [Fact]
        public void ItCanProcessDcLicenseUrlField()
        {
            var ex = new MetdataExtractor();
            var obj = new JObject(new JProperty("url", "http://datadock.io/test/repo/data"),
                new JProperty("dc:license", "http://creativecommons.org/ns#cc-0"));
            var g = new Graph();
            ex.Run(obj, g, new Uri("http://datadock.io/test/repo/publisher"), 100, null);

            var expectSubject = g.CreateUriNode(new Uri("http://datadock.io/test/repo/data"));
            var expectPredicate = g.CreateUriNode(new Uri("http://purl.org/dc/terms/license"));
            var licenseStatement = g.GetTriplesWithSubjectPredicate(expectSubject, expectPredicate).FirstOrDefault();
            licenseStatement.Should().NotBeNull();
            licenseStatement.Object.Should()
                .BeAssignableTo<IUriNode>()
                .Which.Uri.Should()
                .Be(new Uri("http://creativecommons.org/ns#cc-0"));
        }

        [Fact]
        public void ItCanProcessMultipleDcatKeywords()
        {
            var ex = new MetdataExtractor();
            var obj = new JObject(new JProperty("url", "http://datadock.io/test/repo/data"),
                new JProperty("dcat:keyword", new JArray(new JValue("one"), new JValue("two"), new JValue("three"))));
            var g = new Graph();
            ex.Run(obj, g, new Uri("http://datadock.io/test/repo/publisher"), 100, null);

            var expectSubject = g.CreateUriNode(new Uri("http://datadock.io/test/repo/data"));
            var expectPredicate = g.CreateUriNode(new Uri("http://www.w3.org/ns/dcat#keyword"));

            var statements = g.GetTriplesWithSubjectPredicate(expectSubject, expectPredicate).ToList();
            statements.Count.Should().Be(3);
            foreach (var statement in statements)
            {
                statement.Object.Should()
                    .BeAssignableTo<ILiteralNode>()
                    .Which.Value.Should()
                    .BeOneOf("one", "two", "three");
            }
        }

        [Fact]
        public void ItAssertsModifiedDate()
        {
            var ex = new MetdataExtractor();
            var obj = new JObject(new JProperty("url", "http://datadock.io/test/repo/data"),
                new JProperty("dc:license", "http://creativecommons.org/ns#cc-0"));
            var g = new Graph();
            ex.Run(obj, g, new Uri("http://datadock.io/test/repo/publisher"), 100, new DateTime(2017, 01, 02, 03, 04, 05));
            var expectSubject = g.CreateUriNode(new Uri("http://datadock.io/test/repo/data"));
            var expectPredicate = g.CreateUriNode(new Uri("http://purl.org/dc/terms/modified"));

            var statements = g.GetTriplesWithSubjectPredicate(expectSubject, expectPredicate).ToList();
            statements.Count.Should().Be(1);
            var val = statements[0].Object as ILiteralNode;
            val.Should().NotBeNull();
            val.Value.Should().Be("2017-01-02");
            val.DataType.ToString().Should().Be("http://www.w3.org/2001/XMLSchema#date");
        }

        private static void GraphShouldContainRdfTypeStatement(IGraph g, INode expectSubject)
        {
            var rdfType = g.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
            var dataset = g.CreateUriNode(new Uri("http://rdfs.org/ns/void#Dataset"));
            var statements = g.GetTriplesWithSubjectPredicate(expectSubject, rdfType);
            statements.Should()
                .Contain(x => x.Object.Equals(dataset), "Expected subject {0} to have an rdf:type of dcat:Dataset",
                    expectSubject.ToString());
        }

        private static void GraphShouldContainDcPublisherStatement(IGraph g, INode expectSubject, INode expectPublisher)
        {
            var dcPublisher = g.CreateUriNode(new Uri("http://purl.org/dc/terms/publisher"));
            var statements = g.GetTriplesWithSubjectPredicate(expectSubject, dcPublisher);
            statements.Should()
                .Contain(x => x.Object.Equals(expectPublisher), "Expected subject {0} to have a dc:publisher of {1}",
                    expectSubject.ToString(), expectPublisher.ToString());
        }
    }
}
