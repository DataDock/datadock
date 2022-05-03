using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF;

namespace DataDock.Worker.Templating
{
    public class RdfTypeTemplateSelector : ITemplateSelector
    {
        private readonly Uri _typeIri;
        private readonly string _templateName;
        private static readonly Uri TypePredicateIri = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");

        public RdfTypeTemplateSelector(Uri typeIri, string templateName)
        {
            _typeIri = typeIri;
            _templateName = templateName;
        }

        public string SelectTemplate(Uri subjectIri, IList<Triple> triples)
        {
            var typeMatch = triples.Any(t => t.Subject is IUriNode && t.Predicate is IUriNode && t.Object is IUriNode &&
                                             (t.Subject as IUriNode).Uri.Equals(subjectIri) &&
                                             (t.Predicate as IUriNode).Uri.Equals(TypePredicateIri) &&
                                             (t.Object as IUriNode).Uri.Equals(_typeIri));
            return typeMatch ? _templateName : null;
        }
    }
}
