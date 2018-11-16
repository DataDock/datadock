using System;
using FluentAssertions;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class DeleteDatasetSpec : BaseDataDockRepositorySpec
    {
        private readonly Uri _datasetGraphIri;
        private readonly Uri _metadataGraphIri;
        private readonly Uri _rootMetadataGraphIri;

        public DeleteDatasetSpec()
        {
            _datasetGraphIri = new Uri("http://datadock.io/test/repo/example");
            _metadataGraphIri = new Uri("http://datadock.io/test/repo/example/metadata");
            _rootMetadataGraphIri = new Uri("http://datadock.io/test/repo/metadata");
            Repo.DeleteDataset(_datasetGraphIri);
        }

        [Fact]
        public void DeleteDatasetShouldDropDatasetGraph()
        {
            QuinceStore.DroppedGraphs.Should().Contain(_datasetGraphIri);
        }

        [Fact]
        public void DeleteDatasetShouldDropMetadataGraph()
        {
            QuinceStore.DroppedGraphs.Should().Contain(_metadataGraphIri);
        }

        [Fact]
        public void DeleteDatasetShouldRetractVoidSubsetStatement()
        {
            var expect = new Graph();
            expect.Assert(
                expect.CreateUriNode(BaseUri),
                expect.CreateUriNode(new Uri("http://rdfs.org/ns/void#subset")),
                expect.CreateUriNode(_datasetGraphIri));
            QuinceStore.AssertTriplesRetracted(expect.Triples, _rootMetadataGraphIri);
        }

        [Fact]
        public void DeleteDatasetShouldFlushQuinceStore()
        {
            QuinceStore.Flushed.Should().BeTrue();
        }
    }
}
