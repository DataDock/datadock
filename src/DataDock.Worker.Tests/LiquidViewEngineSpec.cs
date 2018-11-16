using System;
using System.Collections.Generic;
using DataDock.Worker.Liquid;
using DataDock.Worker.Templating;
using FluentAssertions;
using Moq;
using NetworkedPlanet.Quince;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class LiquidViewEngineSpec
    {
        private readonly List<Triple> _testTriples;
        private readonly List<Triple> _incomingTriples;
        private readonly List<Triple> _s2Triples;
        public LiquidViewEngineSpec()
        {
            _testTriples = new List<Triple>();
            _incomingTriples = new List<Triple>();

            var g = new Graph();
            var s = g.CreateUriNode(new Uri("http://datadock.io/test/repo/data/s1"));
            var graphUri= new Uri("http://datadock.io/test/repo/data");
            _testTriples.Add(
                new Triple(s, g.CreateUriNode(new Uri("http://example.org/p0")), g.CreateUriNode(new Uri("http://datadock.io/test/repo/data/s2")), graphUri));
            _testTriples.Add(
                new Triple(s, g.CreateUriNode(new Uri("http://example.org/p1")), g.CreateLiteralNode("Simple Literal"), graphUri));
            _testTriples.Add(
                new Triple(s, g.CreateUriNode(new Uri("http://example.org/p2")), g.CreateLiteralNode("Language Tagged Literal", "en"), graphUri));
            _testTriples.Add(
                new Triple(s, g.CreateUriNode(new Uri("http://example.org/p3")), g.CreateLiteralNode("Datatyped Literal", new Uri("http://example.org/datatype"))));

            _incomingTriples.Add(
                new Triple(g.CreateUriNode(new Uri("http://datadock.io/test/repo/data/s2")),
                    g.CreateUriNode(new Uri("http://example.org/p0")), s, graphUri));

            _s2Triples = new List<Triple>();
            var s2 = g.CreateUriNode(new Uri("http://datadock.io/test/repo/data/s1"));
            _s2Triples.Add(new Triple(s2, g.CreateUriNode(new Uri("http://example.org/p1")), g.CreateLiteralNode("Node 2"), graphUri));
        }

        [Fact]
        public void ItRendersUsingTheDefaultTemplate()
        {
            var viewEngine = new LiquidViewEngine();
            var mockStore = Mock.Of<IQuinceStore>();
            viewEngine.Initialize("data", mockStore);
            var result = viewEngine.Render(new Uri("http://datadock.io/test/repo/data/s1"), _testTriples, _incomingTriples);
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().MatchRegex(@"<h1>\s+http://datadock\.io/test/repo/data/s1\s+</h1>");
        }

        [Fact]
        public void ItRendersUsingADifferentDefaultTemplate()
        {
            var viewEngine = new LiquidViewEngine();
            var mockStore = Mock.Of<IQuinceStore>();
            viewEngine.Initialize("data", mockStore, "simple.liquid");
            var result = viewEngine.Render(new Uri("http://datadock.io/test/repo/data/s1"), _testTriples, _incomingTriples);
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().Be(@"http://datadock.io/test/repo/data/s1");
        }

        [Fact]
        public void ItSupportsIteratingNestedTriples()
        {
            var viewEngine = new LiquidViewEngine();
            var mockStore = new Mock<IQuinceStore>();
            mockStore.Setup(
                    m => m.GetTriplesForSubject(It.Is<IUriNode>(u => u.Uri.Equals(new Uri("http://datadock.io/test/repo/data/s2")))))
                .Returns(_s2Triples);
            viewEngine.Initialize("data", mockStore.Object, "nested.liquid");
            var result = viewEngine.Render(new Uri("http://datadock.io/test/repo/data/s1"), _testTriples, _incomingTriples);
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().MatchRegex("Node 2");
        }

        [Fact]
        public void ItSupportsWhereFilterOnPredicate()
        {
            var viewEngine = new LiquidViewEngine();
            var mockStore = Mock.Of<IQuinceStore>();
            viewEngine.Initialize("data", mockStore, "where.liquid");
            var result = viewEngine.Render(new Uri("http://datadock.io/test/repo/data/s1"), _testTriples, _incomingTriples);
            result.Should().NotBeNullOrWhiteSpace();
            result = result.Trim();
            result.Should().Be(@"Simple Literal");
        }

        [Fact]
        public void ItSupportsNamespaces()
        {
            var viewEngine = new LiquidViewEngine();
            var mockStore = Mock.Of<IQuinceStore>();
            viewEngine.Initialize("data", mockStore, "namespaces.liquid");
            var result = viewEngine.Render(new Uri("http://datadock.io/test/repo/data/s1"), _testTriples, _incomingTriples);
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().Contain("http://rdfs.org/ns/void#dataset");
            result.Should().Contain("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
            result.Should().Contain("http://www.w3.org/2000/01/rdf-schema#label");
            result.Should().Contain("http://www.w3.org/2001/XMLSchema#integer");
            result.Should().Contain("http://purl.org/dc/terms/publisher");
            result.Should().Contain("http://xmlns.com/foaf/0.1/name");
        }

        [Fact]
        public void ItUsesTemplateSelectors()
        {
            var viewEngine = new LiquidViewEngine();
            var mockStore = Mock.Of<IQuinceStore>();
            var mockSelector = new Mock<ITemplateSelector>();
            mockSelector.Setup(x => x.SelectTemplate(It.IsAny<Uri>(), It.IsAny<IList<Triple>>()))
                .Returns("simple.liquid")
                .Verifiable("Expected selector to be invoked");
            var secondMockSelector = new Mock<ITemplateSelector>();
            secondMockSelector.Setup(x => x.SelectTemplate(It.IsAny<Uri>(), It.IsAny<IList<Triple>>()))
                .Returns("foo.liquid");
            viewEngine.Initialize("data", mockStore, "default.liquid", new List<ITemplateSelector> {mockSelector.Object, secondMockSelector.Object});
            var result = viewEngine.Render(new Uri("http://datadock.io/test/repo/data/s1"), _testTriples, _incomingTriples);
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().Be(@"http://datadock.io/test/repo/data/s1");
            mockSelector.Verify();
            secondMockSelector.Verify(x=>x.SelectTemplate(It.IsAny<Uri>(), It.IsAny<IList<Triple>>()), Times.Never);
        }
    }
}
