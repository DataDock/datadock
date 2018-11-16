using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataDock.Common;
using Moq;
using NetworkedPlanet.Quince;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class CsvConverionSpec : IDisposable
    {
        private readonly string _tmpDir;
        private readonly IDataDockUriService _uriService;

        public CsvConverionSpec()
        {
            _tmpDir = Path.GetFullPath(DateTime.UtcNow.Ticks.ToString());
            _uriService = new DataDockUriService("http://datadock.io/");
        }

        public void Dispose()
        {
            Directory.Delete(_tmpDir, true);
        }

        [Fact]
        public void CanGenerateRdfFromRepository()
        {
            var repoDir = Path.Combine(_tmpDir, "quince");
            Directory.CreateDirectory(repoDir);
            var repo = new DynamicFileStore(repoDir, 100);
            var defaultGraph = new Graph();
            var parser = new NQuadsParser();
            using (var reader = File.OpenText("data\\test1.nq"))
            {
                parser.Parse(reader, t => repo.Assert(t.Subject, t.Predicate, t.Object, t.GraphUri), defaultGraph);
            }

            repo.Flush();
            var mockQuinceFactory = new Mock<IQuinceStoreFactory>();
            mockQuinceFactory.Setup(x => x.MakeQuinceStore(It.IsAny<string>())).Returns(repo);
            var rdfGeneratorMock = new Mock<ITripleCollectionHandler>();
            rdfGeneratorMock.Setup(x => x.HandleTripleCollection(It.Is<IList<Triple>>(c =>
                c.All(t => (t.Subject as IUriNode).Uri.ToString().Equals("http://example.org/id/resource/s/s0"))))).Verifiable();
            var fileGeneratorFactoryMock = new Mock<IFileGeneratorFactory>();
            fileGeneratorFactoryMock.Setup(x => x.MakeRdfFileGenerator(It.IsAny<IResourceFileMapper>(),
                It.IsAny<IEnumerable<Uri>>(), It.IsAny<IProgressLog>(), It.IsAny<int>())).Returns(rdfGeneratorMock.Object);

            var rdfResourceFileMapper = new ResourceFileMapper(
                new ResourceMapEntry(new Uri("http://example.org/id/"),  "data"));
            var htmlResourceFileMapper = new ResourceFileMapper(
                new ResourceMapEntry(new Uri("http://example.org/id/"), "doc"));
            var ddRepository = new DataDockRepository(
                _tmpDir, new Uri("http://example.org/"), new MockProgressLog(),
                mockQuinceFactory.Object, fileGeneratorFactoryMock.Object, 
                rdfResourceFileMapper, htmlResourceFileMapper, _uriService);
            ddRepository.GenerateRdf(new[] {new Uri("http://example.org/g/g1")});

            // Should be invoked to generate files for subject IRIs
            rdfGeneratorMock.Verify(x => x.HandleTripleCollection(It.Is<IList<Triple>>(c =>
                    c.All(t => (t.Subject as IUriNode).Uri.ToString().Equals("http://example.org/id/resource/s/s0")))),
                Times.Once);
            // Should not be invoked to generate files for object IRIs
            rdfGeneratorMock.Verify(x=>x.HandleTripleCollection(It.Is<IList<Triple>>(
                c=>c.Any(t=>(t.Subject as IUriNode).Uri.ToString().Equals("http://example.org/id/resource/o/o0")))), Times.Never);
        }
    }
}
