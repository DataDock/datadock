using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DataDock.Common;
using Moq;
using NetworkedPlanet.Quince;
using VDS.RDF;

namespace DataDock.Worker.Tests
{

    public class BaseDataDockRepositorySpec : IDisposable
    {
        private readonly DirectoryInfo _testDir;
        protected string RepoPath;
        protected readonly MockQuinceStore QuinceStore;
        protected readonly Uri BaseUri;
        protected readonly DataDockRepository Repo;
        protected readonly Mock<IFileGeneratorFactory> FileGeneratorFactory;
        protected readonly Mock<ITripleCollectionHandler> MockRdfFileGenerator;
        protected readonly Mock<IResourceStatementHandler> MockHtmlFileGenerator;

        public BaseDataDockRepositorySpec()
        {
            var runId = DateTime.UtcNow.Ticks.ToString();
            _testDir = Directory.CreateDirectory(runId);
            RepoPath = _testDir.FullName;

            QuinceStore = new MockQuinceStore();
            var quinceStoreFactory = new Mock<IQuinceStoreFactory>();
            quinceStoreFactory.Setup(x => x.MakeQuinceStore(RepoPath)).Returns(QuinceStore);
            FileGeneratorFactory = new Mock<IFileGeneratorFactory>();
            MockRdfFileGenerator = new Mock<ITripleCollectionHandler>();
            MockRdfFileGenerator.Setup(x => x.HandleTripleCollection(It.IsAny<IList<Triple>>())).Returns(true)
                .Verifiable();
            MockHtmlFileGenerator = new Mock<IResourceStatementHandler>();
            MockHtmlFileGenerator.Setup(x => x.HandleResource(
                    It.IsAny<INode>(), It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>()))
                .Returns(true);
            FileGeneratorFactory.Setup(x => x.MakeRdfFileGenerator(
                    It.IsAny<IResourceFileMapper>(),
                    It.IsAny<IEnumerable<Uri>>(), It.IsAny<IProgressLog>(), It.IsAny<int>()))
                .Returns(MockRdfFileGenerator.Object);
            FileGeneratorFactory.Setup(x => x.MakeHtmlFileGenerator(
                    It.IsAny<IDataDockUriService>(),
                    It.IsAny<IResourceFileMapper>(), It.IsAny<IViewEngine>(), It.IsAny<IProgressLog>(),
                    It.IsAny<int>(),
                    It.IsAny<Dictionary<string, object>>()))
                .Returns(MockHtmlFileGenerator.Object);

            var uriService = new DataDockUriService("http://datadock.io/");
            BaseUri = new Uri("http://datadock.io/test/repo/");
            var rdfResourceMapper = new ResourceFileMapper(
                new ResourceMapEntry(new Uri(BaseUri, "id"), "data"));
            var htmlResourceMapper = new ResourceFileMapper(
                new ResourceMapEntry(new Uri(BaseUri, "id"), "doc"));

            Repo = new DataDockRepository(RepoPath, BaseUri, new MockProgressLog(),
                quinceStoreFactory.Object, FileGeneratorFactory.Object,
                rdfResourceMapper, htmlResourceMapper, uriService);

        }

        public void Dispose()
        {
            _testDir.Delete(true);
        }

    }
}
