using System;
using System.Collections.Generic;
using DataDock.Worker.Templating;
using NetworkedPlanet.Quince;
using VDS.RDF;

namespace DataDock.Worker
{
    public interface IViewEngine
    {
        void Initialize(string templatePath, IQuinceStore store, string defaultTemplateName, IList<ITemplateSelector> templateSelectors);

        string Render(Uri subjectUri, IList<Triple> triples, IList<Triple> incomingTriples, Dictionary<string, object> addVariables = null);
    }
}