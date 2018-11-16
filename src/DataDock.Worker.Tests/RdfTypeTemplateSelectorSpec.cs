using System;
using System.Collections.Generic;
using System.Linq;
using DataDock.Worker.Templating;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class RdfTypeTemplateSelectorSpec
    {
        private readonly List<Triple> _triples;
        public RdfTypeTemplateSelectorSpec()
        {
            var g = new Graph();
            var s = g.CreateUriNode(new Uri("http://example.org/s"));
            var rdfType = g.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
            var t1 = g.CreateUriNode(new Uri("http://example.org/t1"));
            var t2 = g.CreateUriNode(new Uri("http://example.org/t2"));
            var p = g.CreateUriNode(new Uri("http://example.org/p"));
            var o = g.CreateUriNode(new Uri("http://example.org/o"));
            g.Assert(s, rdfType, t1);
            g.Assert(s, p, o);
            g.Assert(s, rdfType, t2);
            _triples = g.Triples.ToList();
        }

        [Theory, MemberData(nameof(MatchData))]
        public void MatchesExactRdfTypeForSubject(Uri subjectIri, Uri typeIri, bool expectMatch)
        {
            var selector = new RdfTypeTemplateSelector(typeIri, "test.template");
            var selectedTemplate = selector.SelectTemplate(subjectIri, _triples);
            if (expectMatch)
            {
                Assert.Equal("test.template", selectedTemplate);
            }
            else
            {
                Assert.Null(selectedTemplate);
            }
        }

        public static IEnumerable<object[]> MatchData => new[]
        {
            new object[] {new Uri("http://example.org/s"), new Uri("http://example.org/t1"), true},
            new object[] {new Uri("http://example.org/s"), new Uri("http://example.org/t2"), true},
            new object[] {new Uri("http://example.org/s"), new Uri("http://example.org/t3"), false},
            new object[] {new Uri("http://example.org/s"), new Uri("http://example.org/o"), false},
            new object[] {new Uri("http://example.org/p"), new Uri("http://example.org/t1"), false}
        };
    }
}
