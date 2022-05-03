using System.Collections.Generic;
using System.Linq;
using DotLiquid;
using NetworkedPlanet.Quince;
using VDS.RDF;

namespace DataDock.Worker.Liquid
{
    public class TripleDrop : Drop
    {
        private readonly Triple _triple;
        private readonly IQuinceStore _store;
        private IList<TripleDrop> _nested;

        public TripleDrop(Triple triple, IQuinceStore store)
        {
            _triple = triple;
            _store = store;
        }

        public string Subject => UriOrBNode(_triple.Subject);
        public bool SubjectIsUri => _triple.Subject is IUriNode;
        public bool SubjectIsBlank => _triple.Subject is IBlankNode;

        public string Predicate => UriOrBNode(_triple.Predicate);
        public bool PredicateIsUri => _triple.Predicate is IUriNode;
        public bool PredicateIsBlank => _triple.Predicate is IBlankNode;


        public bool ObjectIsUri => _triple.Object is IUriNode;
        public bool ObjectIsBlank => _triple.Object is IBlankNode;
        public bool ObjectIsLiteral => _triple.Object is ILiteralNode;
        public string Object => NodeValue(_triple.Object);
        public string ObjectDatatype => GetDatatype(_triple.Object);
        public string ObjectLanguage => GetLanguage(_triple.Object);

        public IEnumerable<TripleDrop> NestedTriples
        {
            get {
                return _nested ??
                       (_nested =
                           _store.GetTriplesForSubject(_triple.Object).Select(t => new TripleDrop(t, _store)).ToList());
            }
        }

        private static string UriOrBNode(INode node)
        {
            var uriNode = node as UriNode;
            if (uriNode != null) return uriNode.Uri.ToString();
            var bnode = node as BlankNode;
            if (bnode != null) return "_:" + bnode.InternalID;
            return null;
        }

        private static string NodeValue(INode node)
        {
            var literal = node as ILiteralNode;
            return literal != null ? literal.Value : UriOrBNode(node);
        }

        private static string GetDatatype(INode node)
        {
            var literal = node as ILiteralNode;
            var ret = literal?.DataType?.ToString();
            return string.IsNullOrWhiteSpace(ret) ? null : ret;
        }

        private static string GetLanguage(INode node)
        {
            var literal = node as ILiteralNode;
            var ret = literal?.Language;
            return string.IsNullOrWhiteSpace(ret) ? null : ret;
        }
    }
}