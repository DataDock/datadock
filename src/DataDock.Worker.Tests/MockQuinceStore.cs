using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NetworkedPlanet.Quince;
using VDS.RDF;

namespace DataDock.Worker.Tests
{
    public class MockQuinceStore: IQuinceStore {

        public List<Uri> DroppedGraphs { get; }
        public List<Tuple<INode, INode, INode, Uri>> Asserted { get; }
        public List<Tuple<INode, INode, INode, Uri>> Retracted { get; }
        private readonly Dictionary<INode, List<Triple>> _triplesBySubject = new Dictionary<INode, List<Triple>>();
        private readonly Dictionary<INode, List<Triple>> _triplesByObject = new Dictionary<INode, List<Triple>>();

        public bool Flushed { get; private set; }

        public MockQuinceStore()
        {
            Asserted = new List<Tuple<INode, INode, INode, Uri>>();
            Retracted = new List<Tuple<INode, INode, INode, Uri>>();
            DroppedGraphs = new List<Uri>();
            Flushed = false;
        }

        public void Assert(INode subject, INode predicate, INode obj, Uri graph)
        {
            Asserted.Add(new Tuple<INode, INode, INode, Uri>(subject, predicate, obj, graph));
            var t = new Triple(subject, predicate, obj, graph);
            if (_triplesBySubject.TryGetValue(subject, out var triples))
            {
                triples.Add(t);
            }
            else
            {
                _triplesBySubject.Add(t.Subject, new List<Triple>{t});
            }
            if (_triplesByObject.TryGetValue(obj, out var triples2))
            {
                triples2.Add(t);
            }
            else
            {
                _triplesByObject.Add(t.Object, new List<Triple> { t });
            }

            Flushed = false;
        }

        public void Retract(INode subject, INode predicate, INode obj, Uri graph)
        {
            Retracted.Add(new Tuple<INode, INode, INode, Uri>(subject, predicate, obj, graph));
            var t = new Triple(subject, predicate, obj, graph);
            if (_triplesBySubject.TryGetValue(subject, out var triples))
            {
                triples.Remove(t);
            }

            if (_triplesByObject.TryGetValue(obj, out var triples2))
            {
                triples2.Remove(t);
            }

            Flushed = false;
        }

        public void Assert(IGraph graph)
        {
            foreach (var t in graph.Triples)
                Asserted.Add(new Tuple<INode, INode, INode, Uri>(t.Subject, t.Predicate, t.Object, graph.BaseUri));
        }

        public void DropGraph(Uri graph)
        {
            DroppedGraphs.Add(graph);
        }

        public void Flush()
        {
            Flushed = true;
        }

        public IEnumerable<Triple> GetTriplesForSubject(INode subjectNode)
        {
            return Asserted.Where(t => t.Item1.Equals(subjectNode))
                .Select(t => new Triple(t.Item1, t.Item2, t.Item3, t.Item4));
        }

        public IEnumerable<Triple> GetTriplesForSubject(Uri subjectUri)
        {
            return Asserted.Where(x => x.Item1 is IUriNode && ((IUriNode)x.Item1).Uri.Equals(subjectUri))
                .Select(x => new Triple(x.Item1, x.Item2, x.Item3, x.Item4));
        }

        public IEnumerable<Triple> GetTriplesForObject(INode objectNode)
        {
            return _triplesByObject.ContainsKey(objectNode) ? _triplesByObject[objectNode] : new List<Triple>(0);
        }

        public IEnumerable<Triple> GetTriplesForObject(Uri objectUri)
        {
            throw new NotImplementedException();
        }

        public void EnumerateSubjects(ITripleCollectionHandler handler)
        {
            foreach (var s in _triplesBySubject.Keys)
            {
                handler.HandleTripleCollection(_triplesBySubject[s]);
            }
        }

        public void EnumerateSubjects(IResourceStatementHandler handler)
        {
            foreach (var s in _triplesBySubject.Keys)
            {
                handler.HandleResource(s, _triplesBySubject[s],
                    _triplesByObject.ContainsKey(s)
                        ? _triplesByObject[s]
                        : new List<Triple>(0));
            }
        }

        public void EnumerateObjects(ITripleCollectionHandler handler)
        {
            foreach (var o in _triplesByObject.Keys)
            {
                handler.HandleTripleCollection(_triplesByObject[o]);
            }
        }

        public void EnumerateObjects(IResourceStatementHandler handler)
        {
            foreach (var o in _triplesByObject.Keys)
            {
                handler.HandleResource(o, _triplesBySubject.ContainsKey(o) ? _triplesBySubject[o] : new List<Triple>(0),
                    _triplesByObject[o]);
            }
        }

        public void AssertTriplesInserted(BaseTripleCollection tripleCollection, Uri graphIri)
        {
            foreach (var t in tripleCollection)
            {
                Asserted.Should().Contain(x =>
                        x.Item1.Equals(t.Subject) && x.Item2.Equals(t.Predicate) && x.Item3.Equals(t.Object) &&
                        x.Item4.Equals(graphIri),
                    "Expected a quad ({0}, {1}, {2}, {3}) to have been asserted but no matching quad was found.",
                    t.Subject, t.Predicate, t.Object, graphIri);
            }
        }

        public void AssertTriplesRetracted(BaseTripleCollection tripleCollection, Uri graphIri)
        {
            foreach (var t in tripleCollection)
            {
                Retracted.Should().Contain(x =>
                        x.Item1.Equals(t.Subject) && x.Item2.Equals(t.Predicate) && x.Item3.Equals(t.Object) &&
                        x.Item4.Equals(graphIri),
                    "Expected a quad ({0}, {1}, {2}, {3}) to have been retracted but no matching quad was found.",
                    t.Subject, t.Predicate, t.Object, graphIri);
            }
        }
    }
}