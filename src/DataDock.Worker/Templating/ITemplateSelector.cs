using System;
using System.Collections.Generic;
using VDS.RDF;

namespace DataDock.Worker.Templating
{
    public interface ITemplateSelector
    {
        string SelectTemplate(Uri subjectIri, IList<Triple> triples);
    }
}
