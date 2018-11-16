using System;
using System.Collections.Generic;
using DataDock.Worker.Liquid;
using FluentAssertions;
using Moq;
using NetworkedPlanet.Quince;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class VoidTemplateSpec
    {
        [Fact]
        public void ItRendersPublisherData()
        {
            var triples = new List<Triple>();
            var g = new Graph();
            g.NamespaceMap.AddNamespace("dcterms", new Uri("http://purl.org/dc/terms/"));
            g.NamespaceMap.AddNamespace("foaf", new Uri("http://xmlns.com/foaf/0.1/"));
            g.NamespaceMap.AddNamespace("void", new Uri("http://rdfs.org/ns/void#"));
            g.NamespaceMap.AddNamespace("dcat", new Uri("http://www.w3.org/ns/dcat#"));
            g.NamespaceMap.AddNamespace("rdf", new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
            var repo = g.CreateUriNode(new Uri("http://datadock.io/networkedplanet/sample"));
            var publisher = g.CreateUriNode(new Uri("http://datadock.io/networkedplanet/sample/id/dataset/publisher"));
            triples.Add(new Triple(repo, g.CreateUriNode("dcterms:publisher"), publisher));
            var dataset1 = g.CreateUriNode(new Uri("http://datadock.io/networkedplanet/sample/id/dataset/dataset1"));
            var dataset2 = g.CreateUriNode(new Uri("http://datadock.io/networkedplanet/sample/id/dataset/dataset2"));
            triples.Add(new Triple(repo, g.CreateUriNode("void:subset"), dataset1));
            triples.Add(new Triple(repo, g.CreateUriNode("void:subset"), dataset2));
            triples.Add(new Triple(repo, g.CreateUriNode("dcterms:title"), g.CreateLiteralNode("Test Repository")));
            triples.Add(new Triple(repo, g.CreateUriNode("dcterms:description"), g.CreateLiteralNode("Test repository description\nwith a newline in it.")));
            var publisherTriples = new List<Triple>
            {
                new Triple(publisher, g.CreateUriNode("foaf:name"), g.CreateLiteralNode("NetworkedPlanet")),
                new Triple(publisher, g.CreateUriNode("foaf:homepage"), g.CreateUriNode(new Uri("http://networkedplanet.com/"))),
                new Triple(publisher, g.CreateUriNode("foaf:mbox"), g.CreateLiteralNode("contact@networkedplanet.com")),
                new Triple(publisher, g.CreateUriNode("rdf:type"), g.CreateUriNode("foaf:Organization"))
            };
            var dataset1Triples = new List<Triple>
            {
                new Triple(dataset1, g.CreateUriNode("dcterms:title"), g.CreateLiteralNode("Dataset 1")),
                new Triple(dataset1, g.CreateUriNode("dcterms:description"), g.CreateLiteralNode("This is a dataset description.")),
                new Triple(dataset1, g.CreateUriNode("dcterms:license"), g.CreateUriNode(new Uri("https://creativecommons.org/publicdomain/zero/1.0/"))),
                new Triple(dataset1, g.CreateUriNode("void:triples"), g.CreateLiteralNode("123", new Uri("http://www.w3.org/2001/XMLSchema#integer"))),
                new Triple(dataset1, g.CreateUriNode("dcat:keyword"), g.CreateLiteralNode("tag1")),
                new Triple(dataset1, g.CreateUriNode("dcat:keyword"), g.CreateLiteralNode("tag2")),
                new Triple(dataset1, g.CreateUriNode("dcterms:modified"), g.CreateLiteralNode("2017-01-02", new Uri("http://www.w3.org/2001/XMLSchema#date"))),
                new Triple(dataset1, g.CreateUriNode("void:dataDump"), g.CreateUriNode(new Uri("https://github.com/kal/lodtest/releases/download/AT.csv_20170106_150624/AT.csv_20170106_150624.nt.gz")))
            };
            var dataset2Triples = new List<Triple>();
            var quince = new Mock<IQuinceStore>();
            quince.Setup(m => m.GetTriplesForSubject(It.Is<IUriNode>(x => x.Uri.Equals(publisher.Uri))))
                .Returns(publisherTriples);
            quince.Setup(m => m.GetTriplesForSubject(It.Is<IUriNode>(x => x.Uri.Equals(dataset1.Uri))))
                .Returns(dataset1Triples);
            quince.Setup(m => m.GetTriplesForSubject(It.Is<IUriNode>(x => x.Uri.Equals(dataset2.Uri))))
                .Returns(dataset2Triples);
            var viewEngine = new LiquidViewEngine();
            viewEngine.Initialize("templates", quince.Object, "void.liquid");
            var result = viewEngine.Render(repo.Uri, triples, new List<Triple>(),
                new Dictionary<string, object>
                {
                    {"nquads", "http://datadock.io/networkedplanet/sample/data/void.nq"},
                    {"datadock-publish-url", "http://datadock.io" }
                });
            result.Should().NotBeNullOrWhiteSpace();
            result.Should()
                .MatchRegex(@"<title>\s*Repository: Test Repository\s*</title>",
                    because: "the repository title should appear in the head");
            result.Should()
                .Contain(
                    @"<link rel=""alternate"" type=""application/n-quads"" href=""http://datadock.io/networkedplanet/sample/data/void.nq""/>",
                    because: "there should be an alternate link to the nquads void file");
            result.Should()
                .MatchRegex(
                    @"<body prefix=""dc: http://purl.org/dc/terms/ void: http://rdfs.org/ns/void# dcat: http://www.w3.org/ns/dcat#"" resource=""http://datadock.io/networkedplanet/sample"">");
            result.Should().MatchRegex(@"<h1>\s*Test Repository\s*</h1>");
            result.Should()
                .MatchRegex(
                    @"<div class=""list"" property=""dc:publisher"" resource=""http://datadock.io/networkedplanet/sample/id/dataset/publisher"" typeof=""http://xmlns.com/foaf/0.1/Organization"">");
            result.Should().MatchRegex(@"<span property=""dc:description"">\s*This is a dataset description.\s*</span>");
            result.Should().MatchRegex(@"<span property=""foaf:name"">NetworkedPlanet</span>");
            result.Should()
                .MatchRegex(
                    @"<a property=""foaf:homepage"" href=""http://networkedplanet.com/"">http://networkedplanet\.com/</a>");
            result.Should().MatchRegex(@"<span property=""foaf:mbox"">contact@networkedplanet\.com</span>");

            result.Should()
                .MatchRegex(
                    @"<div class=""card"" property=""void:subset"" resource=""http://datadock.io/networkedplanet/sample/id/dataset/dataset1"">");
            result.Should()
                .MatchRegex(
                    @"<div class=""content"" about=""http://datadock.io/networkedplanet/sample/id/dataset/dataset1"">");
            result.Should().MatchRegex(@"<h3 property=""dc:title"">\s*<a href=""http://datadock.io/networkedplanet/sample/id/dataset/dataset1"">Dataset 1\s*</a>\s*</h3>", because:"there should be a H3 for Dataset 1");
            result.Should()
                .MatchRegex(@"<dd>\s+<span property=""dc:description"">This is a dataset description.</span>\s+</dd>",
                    because: "there is a description for dataset 1");
            result.Should()
                .MatchRegex(
                    @"<dt>License:</dt>\s+<dd>\s*<a property=""dc:license"" href=""https://creativecommons.org/publicdomain/zero/1.0/"">CC-0 \(Public Domain\)</a>\s*</dd>",
                    because: "dataset 1 uses CC-0 as a license");
            //result.Should().MatchRegex(@"<td>Triple Count:</td>\s+<td>\s+<span property=""void:triples"" datatype=""http://www.w3.org/2001/XMLSchema#integer"">123</span>\s+</td>",because: "the triple count for dataset 1 is 123");
            result.Should()
                .MatchRegex(@"<a href=""http://datadock.io/search\?tag=tag1"" class=""ui tag label""><span about=""http://datadock.io/networkedplanet/sample/id/dataset/dataset1"" property=""dcat:keyword"">tag1</span></a>",
                    because: "there should be a keyword tag1 for datatset 1");
            result.Should()
                .MatchRegex(@"<a href=""http://datadock.io/search\?tag=tag2"" class=""ui tag label""><span about=""http://datadock.io/networkedplanet/sample/id/dataset/dataset1"" property=""dcat:keyword"">tag2</span></a>",
                    because: "there should be a keyword tag2 for datatset 1");
            result.Should()
                .MatchRegex(@"<dt>Modified:</dt>\s*<dd>\s*<span property=""dc:modified"" datatype=""http://www.w3.org/2001/XMLSchema#date"">2017-01-02</span>\s*</dd>",
                    because: "there should be a modified date for dataset 1");
            result.Should()
                .MatchRegex(
                    @"<a property=""void:dataDump""\s+class=""ui primary button mr""\s+href=""https://github.com/kal/lodtest/releases/download/AT.csv_20170106_150624/AT.csv_20170106_150624.nt.gz""><i class=""download icon""></i>N-QUADS</a>",
                    because: "there should be a download link for dataset 1");


            result.Should()
                .MatchRegex(
                    @"<div class=""card"" property=""void:subset"" resource=""http://datadock.io/networkedplanet/sample/id/dataset/dataset2"">");
            result.Should()
                .MatchRegex(
                    @"<div class=""content"" about=""http://datadock.io/networkedplanet/sample/id/dataset/dataset2"">");
            result.Should()
                .MatchRegex(@"<h3 property=""dc:title"">\s*<a href=""http://datadock.io/networkedplanet/sample/id/dataset/dataset2"">\s*http://datadock.io/networkedplanet/sample/id/dataset/dataset2\s*</a>\s*</h3>",
                    because: "there is no explicit title for dataset 2");
            result.Should().MatchRegex(@"<dt>Description:</dt>\s+<dd>\s*Not specified.\s*</dd>", because: "there is no description for dataset 2");
            result.Should()
                .MatchRegex(@"<dt>License:</dt>\s+<dd>\s+Not specified.\s+</dd>",
                    because: "there is no license for dataset 2");
            result.Should()
                .MatchRegex(@"<div class=""value"">\s+Not specified\.\s+</div>\s+<div class=""label"">\s+Triples\s+</div>",
                    because: "there is no triple count for dataset 2");

        }
    }
}
