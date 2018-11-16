using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataDock.Worker.Liquid;
using Moq;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class DataDockRepositoryPublishTests : BaseDataDockRepositorySpec
    {
        private Uri _datasetGraphIri;
        private Uri _publishedSubject;
        public DataDockRepositoryPublishTests()
        {
            _datasetGraphIri = new Uri("http://datadock.io/test/repo/dataset");
            var initGraph = new Graph();
            _publishedSubject = new Uri("http://datadock.io/test/repo/id/subject");
            var s = initGraph.CreateUriNode(_publishedSubject);
            QuinceStore.TripleCollections.Add(new List<Triple>
            {
                new Triple(s, initGraph.CreateUriNode(new Uri("http://example.org/p1")),
                    initGraph.CreateUriNode(new Uri("http://example.org/o1")))
            });
            QuinceStore.ResourceStatements.Add(
                new Tuple<INode, IList<Triple>, IList<Triple>>(
                    s,
                    QuinceStore.TripleCollections[0],
                    new List<Triple>()));
        }

        [Fact]
        public void PublishInvokesRdfFileGenerator()
        {
            Repo.Publish();
            MockRdfFileGenerator.Verify(
                x => x.HandleTripleCollection(
                    It.Is<IList<Triple>>(
                        triples => triples.All(
                            t => ((IUriNode) t.Subject).Uri.Equals(_publishedSubject))
                    )),
                Times.Once);
        }

        [Fact]
        public void PublishInvokesHtmlFileGenerator()
        {
            Repo.Publish();
            MockHtmlFileGenerator.Verify(
                x => x.HandleResource(
                    It.Is<INode>(n => (n as IUriNode).Uri.Equals(_publishedSubject)),
                    It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>()), 
                Times.Once);
        }

        [Fact]
        public void PublishWithTemplateInvokesHtmlFileGenerator()
        {
            var portalInfo = new PortalInfoDrop {OwnerId = "git-user", RepositoryName = "repo-id", OwnerDisplayName = "Git User"};
            var templateVariables =
                new Dictionary<string, object>
                {
                    {"ownerId", portalInfo?.OwnerId},
                    {"repoName", portalInfo?.RepositoryName},
                    {"portalInfo", portalInfo},
                };

            Repo.Publish(null, templateVariables);
            MockHtmlFileGenerator.Verify(
                x => x.HandleResource(
                    It.Is<INode>(n => (n as IUriNode).Uri.Equals(_publishedSubject)),
                    It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>()),
                Times.Once);
        }
    }
}
