using System;
using System.Collections.Generic;
using System.IO;
using DataDock.Worker.Liquid;
using FluentAssertions;
using Moq;
using NetworkedPlanet.Quince;
using VDS.RDF;
using VDS.RDF.Parsing;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class DefaultTemplateSpec : IClassFixture<DatasetContextFixture>
    {
        private readonly DatasetContextFixture _fixture;

        public DefaultTemplateSpec(DatasetContextFixture fixture)
        {
            _fixture = fixture;
        }
        
        [Fact]
        public void ItDefaultsToTheSubjectIriAsThePageTitle()
        {
            _fixture.RenderResult.Should()
                .MatchRegex(@"<title>\s*http://datadock.io/networkedplanet/test/dataset.csv/id/resource/row_1\s*</title>");
        }

        [Fact]
        public void ItDefaultsToTheSubjectIriAsThePageH1()
        {
            _fixture.RenderResult.Should()
                .MatchRegex(@"<h1>\s*http://datadock.io/networkedplanet/test/dataset.csv/id/resource/row_1\s*</h1>");
        }

        [Fact]
        public void ItProvidesAnAlternateLinkToNQuadsRepresentation()
        {
            _fixture.RenderResult.Should()
                .MatchRegex(
                    @"<link rel=""alternate"" type=""application/n-quads"" href=""http://datadock.io/networkedplanet/test/dataset.csv/data/resource/row_1.nq""/>");
        }

        [Fact]
        public void UriPredicatesAreGeneratedAsLinks()
        {
            _fixture.RenderResult.Should().MatchRegex(
                @"<a href=""http://datadock.io/networkedplanet/test/dataset.csv/id/definition/stringLiteralProperty"">\s*stringLiteralProperty\s*</a>");
        }

        [Fact]
        public void LiteralValuesAreRenderedAsPlainText()
        {
            _fixture.RenderResult.Should().MatchRegex(
                @"<td>\s*<span property=""http://datadock.io/networkedplanet/test/dataset.csv/id/definition/stringLiteralProperty""\s*>\s*Literal Value\s*</span>\s*</td>");
        }

        [Fact]
        public void ResourceValuesAreRenderedAsLinks()
        {
            _fixture.RenderResult.Should()
                .MatchRegex(
                    @"<td>\s*<a property=""http://datadock.io/networkedplanet/test/dataset.csv/id/definition/refProperty"" href=""http://datadock.io/networkedplanet/test/anotherdataset.csv/id/resource/foo"">http://datadock.io/networkedplanet/test/anotherdataset.csv/id/resource/foo</a>");
        }

        [Fact]
        public void LinkToNQuadsDownloadIsRendered()
        {
            _fixture.RenderResult.Should()
                .MatchRegex(
                    @"<a href=""http://datadock.io/networkedplanet/test/dataset.csv/data/resource/row_1.nq"">NQuads</a>");
        }

        [Fact(Skip = "This test currently fails, but the W3C RDFa validator says we are generating valid RDFa. Might be an issue with dotNetRDF.")]
        public void ResultIsRdfa()
        {
            var parser = new RdfAParser();
            var g= new Graph();
            using (var reader = new StringReader(_fixture.RenderResult))
            {
                parser.Load(g, reader);
            }
            g.Triples.Should().HaveCount(3);
        }
    }

    public class DatasetContextFixture
    {
        public string RenderResult { get; private set; }
        public List<Triple> Triples { get; private set; }
        public List<Triple> IncomingTriples { get; private set; }

        public DatasetContextFixture()
        {
            Triples = new List<Triple>();
            IncomingTriples = new List<Triple>();
            var g = new Graph();
            g.NamespaceMap.AddNamespace("res", new Uri("http://datadock.io/networkedplanet/test/dataset.csv/id/resource/"));
            g.NamespaceMap.AddNamespace("def", new Uri("http://datadock.io/networkedplanet/test/dataset.csv/id/definition/"));
            var subject = g.CreateUriNode("res:row_1");
            Triples.Add(new Triple(subject, g.CreateUriNode("def:stringLiteralProperty"), g.CreateLiteralNode("Literal Value")));
            Triples.Add(new Triple(subject, g.CreateUriNode("def:dateTimeLiteralProperty"), g.CreateLiteralNode("2017-11-01T10:50:01Z", new Uri("http://www.w3.org/2001/XMLSchema#dateTime"))));
            Triples.Add(new Triple(subject, g.CreateUriNode("def:refProperty"), g.CreateUriNode(new Uri("http://datadock.io/networkedplanet/test/anotherdataset.csv/id/resource/foo"))));
            var viewEngine = new LiquidViewEngine();
            var quince= new Mock<IQuinceStore>();
            viewEngine.Initialize("templates", quince.Object, "default.liquid");
            RenderResult = viewEngine.Render(subject.Uri, Triples, IncomingTriples,
                new Dictionary<string, object>
                {
                    {"nquads", "http://datadock.io/networkedplanet/test/dataset.csv/data/resource/row_1.nq"}
                });

        }
    }
}
