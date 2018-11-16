using System;
using System.Collections.Generic;
using System.IO;
using DataDock.Common;
using DataDock.Worker;
using FluentAssertions;
using Moq;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class HtmlFileGeneratorSpec
    {
        private readonly IGraph _g;
        private readonly IDataDockUriService _uriService;

        public HtmlFileGeneratorSpec()
        {
            _g = new Graph();
            _uriService = new DataDockUriService("http://datadock.io/");
        }

        [Fact]
        public void GeneratesNothingWhenTheRepositoryUriIsNotABaseUriOfTheSubject()
        {
            var viewEngineMock = new Mock<IViewEngine>();
            viewEngineMock.Setup(x => x.Render(It.IsAny<Uri>(), It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>(), null)).Verifiable();
            var resourceMapperMock = new Mock<IResourceFileMapper>();
            var s = new Uri("http://datadock.io/someother/repo/data/s0");
            resourceMapperMock.Setup(x => x.GetPathFor(s)).Returns((string)null).Verifiable();
            var templateVariables = new Dictionary<string, object>();
            var htmlGenerator = new HtmlFileGenerator(_uriService, resourceMapperMock.Object, viewEngineMock.Object, new MockProgressLog(), 100, templateVariables);

            htmlGenerator.HandleResource(_g.CreateUriNode(s), GetMockTripleCollection(s), GetMockObjectTripleCollection(s));

            resourceMapperMock.Verify();
            viewEngineMock.Verify(x=>x.Render(It.IsAny<Uri>(), It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>(), null), Times.Never);
        }

        [Fact]
        public void GeneratesNothingWhenTheSubjectIsABlankNode()
        {
            var viewEngineMock = new Mock<IViewEngine>();
            viewEngineMock.Setup(x => x.Render(It.IsAny<Uri>(), It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>(), null)).Verifiable();
            var resourceMapperMock = new Mock<IResourceFileMapper>();
            resourceMapperMock.Setup(x => x.GetPathFor(null)).Returns((string)null).Verifiable();
            var tripleCollection = GetMockTripleCollection(null);
            var templateVariables = new Dictionary<string, object>();
            var htmlGenerator = new HtmlFileGenerator(_uriService, resourceMapperMock.Object, viewEngineMock.Object, new MockProgressLog(), 100, templateVariables);

            htmlGenerator.HandleResource(tripleCollection[0].Subject, tripleCollection, new List<Triple>());
            resourceMapperMock.Verify(x=>x.GetPathFor(null), Times.Once);
            viewEngineMock.Verify(x => x.Render(It.IsAny<Uri>(), It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>(), null), Times.Never);
        }

        [Fact]
        public void InvokesTheViewEngineWhenTheReposoitoryUriIsABaseUriOfTheSubject()
        {
            var viewEngineMock = new Mock<IViewEngine>();
            viewEngineMock.Setup(x => x.Render(It.IsAny<Uri>(), It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>(), null)).Verifiable();
            var resourceMapperMock = new Mock<IResourceFileMapper>();
            resourceMapperMock.Setup(x => x.GetPathFor(It.IsAny<Uri>())).Returns("data\\s0").Verifiable();
            var s = new Uri("http://datadock.io/test/repo/data/s0");
            var templateVariables = new Dictionary<string, object>();
            var htmlGenerator = new HtmlFileGenerator(_uriService, resourceMapperMock.Object, viewEngineMock.Object,
                new MockProgressLog(), 100, templateVariables);

            htmlGenerator.HandleResource(_g.CreateUriNode(s), GetMockTripleCollection(s), GetMockObjectTripleCollection(s));

            resourceMapperMock.Verify();
            viewEngineMock.Verify(x => x.Render(It.IsAny<Uri>(), It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>(), It.IsAny<Dictionary<string, object>>()), Times.Exactly(1));
        }

        [Fact]
        public void CreatesTheTargetDirectoryForTheOutputHtml()
        {
            var directory = new DirectoryInfo(Path.Combine("tmp", "data", "nested", "path"));
            if (directory.Exists) directory.Delete(true);

            var viewEngineMock = new Mock<IViewEngine>();
            viewEngineMock.Setup(x => x.Render(It.IsAny<Uri>(), It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>(), null)).Verifiable();
            var resourceMapperMock = new Mock<IResourceFileMapper>();
            var s = new Uri("http://datadock.io/test/repo/data/nested/path/s0");
            resourceMapperMock.Setup(x => x.GetPathFor(s)).Returns("tmp\\data\\nested\\path\\s0").Verifiable();
            var templateVariables = new Dictionary<string, object>();
            var htmlGenerator = new HtmlFileGenerator(_uriService, resourceMapperMock.Object, viewEngineMock.Object, new MockProgressLog(), 100, templateVariables);

            htmlGenerator.HandleResource(_g.CreateUriNode(s), GetMockTripleCollection(s), GetMockObjectTripleCollection(s));

            viewEngineMock.Verify(x => x.Render(It.IsAny<Uri>(), It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>(), It.IsAny<Dictionary<string, object>>()), Times.Exactly(1));
            resourceMapperMock.Verify();
            directory = new DirectoryInfo(Path.Combine("tmp", "data", "nested", "path"));
            directory.Exists.Should().BeTrue(because:$"The target directory {directory.FullName} should exist");
        }

        [Fact]
        public void WritesTheRenderedTextToTheTargetHtmlFile()
        {
            var file = new FileInfo(Path.Combine("tmp", "data", "s1.html"));
            if (file.Exists) file.Delete();

            var viewEngineMock = new Mock<IViewEngine>();
            viewEngineMock.Setup(x => x.Render(It.IsAny<Uri>(), It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>(), It.IsAny<Dictionary<string, object>>())).Returns("Results of the render").Verifiable();
            var resourceMapperMock = new Mock<IResourceFileMapper>();
            var s = new Uri("http://datadock.io/test/repo/data/s1");
            resourceMapperMock.Setup(x => x.GetPathFor(s)).Returns("tmp\\data\\s1").Verifiable();
            var templateVariables = new Dictionary<string, object>();
            var htmlGenerator = new HtmlFileGenerator(_uriService, resourceMapperMock.Object, viewEngineMock.Object, new MockProgressLog(), 100, templateVariables);

            htmlGenerator.HandleResource(_g.CreateUriNode(s), GetMockTripleCollection(s), GetMockObjectTripleCollection(s));

            viewEngineMock.Verify(x => x.Render(It.IsAny<Uri>(), It.IsAny<IList<Triple>>(), It.IsAny<IList<Triple>>(), It.IsAny<Dictionary<string, object>>()), Times.Exactly(1));
            file = new FileInfo(Path.Combine("tmp", "data", "s1.html"));
            file.Exists.Should().BeTrue();
            File.ReadAllText(file.FullName).Should().Be("Results of the render");
        }


        private IList<Triple> GetMockTripleCollection(Uri subjecUri)
        {
            var triples = new List<Triple>();
            var s = subjecUri == null ? (INode)_g.CreateBlankNode(): _g.CreateUriNode(subjecUri);
            triples.Add(
                new Triple(
                    s, 
                    _g.CreateUriNode(new Uri("http://datadock.io/test/repo/data/p0")),
                    _g.CreateUriNode(new Uri("http://datadock.io/test/repo/data/o0")), 
                    new Uri("http://datadock.io/test/repo/data")));
            return triples;
        }

        private IList<Triple> GetMockObjectTripleCollection(Uri objectUri)
        {
            var triples = new List<Triple>();
            var o = _g.CreateUriNode(objectUri);
            triples.Add(new Triple(_g.CreateUriNode(new Uri("http://datadock.io/test/repo/data/testSubject")),
                _g.CreateUriNode(new Uri("http://datadock.io/test/repo/data/testPredicate")),
                o,
                new Uri("http://datadock.io/test/repo/data")));
            return triples;
        }

    }
}
