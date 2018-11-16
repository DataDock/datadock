using System;
using DataDock.Common.Models;
using FluentAssertions;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class DataDockRepositoryUpdateSpec : BaseDataDockRepositorySpec
    {
        private readonly IGraph _insertGraph;
        private readonly IGraph _metadataGraph;
        private readonly IGraph _definitionsGraph;
        private readonly Uri _datasetGraphIri;
        private readonly Uri _metadataGraphIri;
        private readonly Uri _definitionsGraphIri;
        private readonly Uri _publisherIri;
        private readonly ContactInfo _publisherInfo;
        private readonly string _repositoryTitle;
        private readonly string _repositoryDescription;
        private readonly Uri _rootMetadataGraphIri;

        public DataDockRepositoryUpdateSpec()
        {
            _insertGraph = new Graph();
            _insertGraph.Assert(_insertGraph.CreateUriNode(new Uri("http://example.org/s")),
                _insertGraph.CreateUriNode(new Uri("http://example.org/p")),
                _insertGraph.CreateUriNode(new Uri("http://example.org/o")));
            _datasetGraphIri = new Uri("http://datadock.io/test/repo/example");
            _metadataGraph = new Graph();
            _metadataGraph.Assert(
                _metadataGraph.CreateUriNode(_datasetGraphIri),
                _metadataGraph.CreateUriNode(new Uri("http://example.org/properties/foo")),
                _metadataGraph.CreateLiteralNode("foo"));
            _metadataGraphIri= new Uri("http://datadock.io/test/repo/example/metadata");
            _definitionsGraph = new Graph();
            _definitionsGraph.Assert(
                _definitionsGraph.CreateUriNode(_datasetGraphIri),
                _definitionsGraph.CreateUriNode(new Uri("http://example.org/properties/bar")),
                _definitionsGraph.CreateLiteralNode("bar"));
            _definitionsGraphIri = new Uri("http://datadock.io/test/repo/example/definitions");
            _publisherIri = new Uri("http://datadock.io/test/publisher");
            _publisherInfo= new ContactInfo { Label = "Test Publisher" };
            _repositoryTitle = "Test Repository";
            _repositoryDescription = "Test Repository Description";
            _rootMetadataGraphIri = new Uri("http://datadock.io/test/repo/metadata");

            // Create some existing triples that we will expect to be retracted
            var initGraph = new Graph();
            initGraph.NamespaceMap.AddNamespace("test", new Uri("http://datadock.io/test/"));
            initGraph.NamespaceMap.AddNamespace("dcterms", new Uri("http://purl.org/dc/terms/"));
            initGraph.NamespaceMap.AddNamespace("rdfs", new Uri("http://www.w3.org/2000/01/rdf-schema#"));
            var repo = initGraph.CreateUriNode(BaseUri);
            var publisher = initGraph.CreateUriNode(_publisherIri);
            QuinceStore.Assert(
                repo,
                initGraph.CreateUriNode("dcterms:title"),
                initGraph.CreateLiteralNode("Old Title"),
                _rootMetadataGraphIri);
            QuinceStore.Assert(
                repo,
                initGraph.CreateUriNode("dcterms:description"),
                initGraph.CreateLiteralNode("Old Description"),
                _rootMetadataGraphIri);
            QuinceStore.Assert(
                publisher,
                initGraph.CreateUriNode("rdfs:label"),
                initGraph.CreateLiteralNode("Old Publisher"),
                _rootMetadataGraphIri);
        }

        [Fact]
        public void UpdateAssertsDataTriples()
        {
            Repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            QuinceStore.AssertTriplesInserted(_insertGraph.Triples, _datasetGraphIri);
        }

        [Fact]
        public void UpdateCanDropDatasetGraph()
        {
            Repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            QuinceStore.AssertTriplesInserted(_insertGraph.Triples, _datasetGraphIri);
            QuinceStore.DroppedGraphs.Should().Contain(_datasetGraphIri);
        }

        [Fact]
        public void UpdateCanAppendToExistingDatasetGraph()
        {
            Repo.UpdateDataset(_insertGraph, _datasetGraphIri, false,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            QuinceStore.AssertTriplesInserted(_insertGraph.Triples, _datasetGraphIri);
            QuinceStore.DroppedGraphs.Should().NotContain(_datasetGraphIri);
        }

        [Fact]
        public void UpdateDropsMetadataGraph()
        {
            Repo.UpdateDataset(_insertGraph, _datasetGraphIri, false,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            QuinceStore.AssertTriplesInserted(_insertGraph.Triples, _datasetGraphIri);
            QuinceStore.DroppedGraphs.Should().Contain(_metadataGraphIri);
        }

        [Fact]
        public void UpdateAssertsMetadataGraphTriples()
        {
            Repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            QuinceStore.AssertTriplesInserted(_metadataGraph.Triples, _metadataGraphIri);
        }

        [Fact]
        public void UpdateAssertsDefinitionsGraphTriples()
        {
            Repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            QuinceStore.AssertTriplesInserted(_metadataGraph.Triples, _metadataGraphIri);
        }

        [Fact]
        public void UpdateAddsMetadataToRootMetadataGraph()
        {
            Repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            var expectedRootMetadata = new Graph();
            // Expectations
            var repoNode = expectedRootMetadata.CreateUriNode(BaseUri);
            // (repo, rdf:type, void:Dataset)
            expectedRootMetadata.Assert(
                repoNode,
                expectedRootMetadata.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")),
                expectedRootMetadata.CreateUriNode(new Uri("http://rdfs.org/ns/void#Dataset")));
            // (repo, void:subset, dataset)
            expectedRootMetadata.Assert(
                repoNode,
                expectedRootMetadata.CreateUriNode(new Uri("http://rdfs.org/ns/void#subset")),
                expectedRootMetadata.CreateUriNode(_datasetGraphIri));
            // (repo, dcterms:publisher, publisher)
            expectedRootMetadata.Assert(
                repoNode,
                expectedRootMetadata.CreateUriNode(new Uri("http://purl.org/dc/terms/publisher")),
                expectedRootMetadata.CreateUriNode(_publisherIri));

            QuinceStore.AssertTriplesInserted(expectedRootMetadata.Triples, _rootMetadataGraphIri);
        }

        [Fact]
        public void UpdateAssertsTitleAndDescription()
        {
            Repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            var expect = new Graph();
            expect.NamespaceMap.AddNamespace("dcterms", new Uri("http://purl.org/dc/terms/"));
            var repoNode = expect.CreateUriNode(BaseUri);
            expect.Assert(repoNode, expect.CreateUriNode("dcterms:title"), expect.CreateLiteralNode(_repositoryTitle));
            expect.Assert(repoNode, expect.CreateUriNode("dcterms:description"), expect.CreateLiteralNode(_repositoryDescription));
            QuinceStore.AssertTriplesInserted(expect.Triples, _rootMetadataGraphIri);
        }

        [Fact]
        public void UpdateRetractsOldTitleAndDescription()
        {
            Repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            var expect = new Graph();
            expect.NamespaceMap.AddNamespace("dcterms", new Uri("http://purl.org/dc/terms/"));
            var repoNode = expect.CreateUriNode(BaseUri);
            expect.Assert(repoNode, expect.CreateUriNode("dcterms:title"), expect.CreateLiteralNode("Old Title"));
            expect.Assert(repoNode, expect.CreateUriNode("dcterms:description"), expect.CreateLiteralNode("Old Description"));
            QuinceStore.AssertTriplesRetracted(expect.Triples, _rootMetadataGraphIri);
        }

        [Fact]
        public void UpdateRetractsOldPublisher()
        {
            Repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            var expect = new Graph();
            expect.NamespaceMap.AddNamespace("dcterms", new Uri("http://purl.org/dc/terms/"));
            expect.NamespaceMap.AddNamespace("rdfs", new Uri("http://www.w3.org/2000/01/rdf-schema#"));
            var publisher = expect.CreateUriNode(_publisherIri);
            expect.Assert(publisher, expect.CreateUriNode("rdfs:label"), expect.CreateLiteralNode("Old Publisher"));
            QuinceStore.AssertTriplesRetracted(expect.Triples, _rootMetadataGraphIri);
        }

        [Fact]
        public void UpdateFlushesQuinceStore()
        {
            Repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            QuinceStore.Flushed.Should().BeTrue();
        }
    }
}
