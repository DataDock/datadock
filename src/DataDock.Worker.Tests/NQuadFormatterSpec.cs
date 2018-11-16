using System;
using FluentAssertions;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class NQuadFormatterSpec
    {
        private readonly IGraph _g;
        private readonly NQuadFormatter _formatter;

        public NQuadFormatterSpec()
        {
            _g = new Graph();
            _formatter = new NQuadFormatter();
        }

        [Fact]
        public void SpacesInIrisAreEscaped()
        {
            string quad = _formatter.FormatQuad(new Triple(_g.CreateUriNode(new Uri("http://example.org/foo bar")),
                _g.CreateUriNode(new Uri("http://example.org/p")),
                _g.CreateLiteralNode("some string"),
                new Uri("http://example.org/g")));

            quad.Should()
                .Be(
                    "<http://example.org/foo\\u0020bar> <http://example.org/p> \"some string\" <http://example.org/g>.");
        }
    }
}
