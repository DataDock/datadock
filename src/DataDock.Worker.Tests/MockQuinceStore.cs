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
        public List<List<Triple>> TripleCollections { get; }
        public List<Tuple<INode, IList<Triple>, IList<Triple>>> ResourceStatements { get; }

        public bool Flushed { get; private set; }

        public MockQuinceStore()
        {
            Asserted = new List<Tuple<INode, INode, INode, Uri>>();
            Retracted = new List<Tuple<INode, INode, INode, Uri>>();
            DroppedGraphs = new List<Uri>();
            TripleCollections = new List<List<Triple>>();
            ResourceStatements = new List<Tuple<INode, IList<Triple>, IList<Triple>>>();
            Flushed = false;
        }

        public void Assert(INode subject, INode predicate, INode obj, Uri graph)
        {
            Asserted.Add(new Tuple<INode, INode, INode, Uri>(subject, predicate, obj, graph));
        }

        public void Retract(INode subject, INode predicate, INode obj, Uri graph)
        {
            Retracted.Add(new Tuple<INode, INode, INode, Uri>(subject, predicate, obj, graph));
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
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesForObject(Uri objectUri)
        {
            throw new NotImplementedException();
        }

        public void EnumerateSubjects(ITripleCollectionHandler handler)
        {
            foreach (var tc in TripleCollections)
            {
                handler.HandleTripleCollection(tc);
            }
        }

        public void EnumerateSubjects(IResourceStatementHandler handler)
        {
            foreach (var resourceStatement in ResourceStatements)
            {
                handler.HandleResource(resourceStatement.Item1, resourceStatement.Item2, resourceStatement.Item3);
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