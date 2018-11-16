using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataDock.Common.Tests
{
    public class DataDockUriServiceTests
    {
        [Fact]
        public void ItProvidesAValidIdentifierRegularExpression()
        {
            var s = new DataDockUriService("http://example.org/");
            s.IdentifierRegex.ToString().Should().Be("^http://example\\.org/([^/]+)/([^/]+)/id/(.*)");
        }

        [Fact]
        public void ItAddsATrailingSlashToThePublishSite()
        {
            var s = new DataDockUriService("http://example.org");
            s.PublishSite.Should().Be("http://example.org/");
        }

        [Fact]
        public void ItDoesNotAddATrailingSlashToThePublishSiteIfOneWasProvidedInTheConstructor()
        {
            var s = new DataDockUriService("http://example.org/");
            s.PublishSite.Should().Be("http://example.org/");
        }

        [Fact]
        public void ItGeneratesAValidRepositoryBaseUri()
        {
            var s = new DataDockUriService("http://example.org");
            s.GetRepositoryUri("owner", "repo").Should().Be("http://example.org/owner/repo/");
        }

        [Fact]
        public void ItGeneratesAValidIdentifierPrefix()
        {
            var s = new DataDockUriService("http://example.org/");
            s.GetIdentifierPrefix("owner", "repo").Should().Be("http://example.org/owner/repo/id/");
        }

        [Fact]
        public void ItGeneratesAValidDatasetIdentifier()
        {
            var s = new DataDockUriService("http://example.org/");
            s.GetDatasetIdentifier("owner", "repo", "data").Should()
                .Be("http://example.org/owner/repo/id/dataset/data");
        }

        [Fact]
        public void ItGeneratesAValidPublisherIdentifier()
        {
            var s = new DataDockUriService("http://example.org/");
            s.GetRepositoryPublisherIdentifier("owner", "repo").Should()
                .Be("http://example.org/owner/repo/id/dataset/publisher");
        }

        [Fact]
        public void ItGeneratesAValidMetadataGraphIdentifier()
        {
            var s = new DataDockUriService("http://example.org/");
            s.GetMetadataGraphIdentifier("owner", "repo").Should().Be("http://example.org/owner/repo/metadata");
        }

        [Fact]
        public void ItGeneratesAValidDefinitionsGraphIdentifier()
        {
            var s = new DataDockUriService("http://example.org/");
            s.GetDefinitionsGraphIdentifier("owner", "repo").Should().Be("http://example.org/owner/repo/definitions");
        }

        [Theory]
        [InlineData("http://example.org/owner/repo/id/foo", "nq", "http://example.org/owner/repo/data/foo.nq")]
        [InlineData("http://example.org/owner/repo/id/foo/bar", "rdf", "http://example.org/owner/repo/data/foo/bar.rdf")]
        [InlineData("http://example.org/owner/repo/id/foo/bar", null, "http://example.org/owner/repo/data/foo/bar")]
        public void ItConvertsSubjectUrisToResourceUrls(string subjectIri, string fileSuffix, string expectUrl)
        {
            var s=  new DataDockUriService("http://example.org/");
            s.GetSubjectDataUrl(subjectIri, fileSuffix).Should().Be(expectUrl);
        }

        [Fact]
        public void NullOrEmptyBaseUriIsNotAllowed()
        {
            Action act = () => new DataDockUriService(null);
            act.Should().Throw<ArgumentException>().Where(e=>e.Message.StartsWith("Base URI must be a non-null non-empty string"));
            act = () => new DataDockUriService(string.Empty);
            act.Should().Throw<ArgumentException>().Where(e=>e.Message.StartsWith("Base URI must be a non-null non-empty string"));

        }
    }
}
