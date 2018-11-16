using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataDock.Worker.Templating;
using DotLiquid;
using NetworkedPlanet.Quince;
using VDS.RDF;

namespace DataDock.Worker.Liquid
{
    public class LiquidViewEngine:IViewEngine
    {
        private string _templatePath;
        private string _defaultTemplateName;
        private Dictionary<string, Template> _parsedTemplates;
        private Dictionary<string, object> _namespaces;
        private IQuinceStore _store;
        private IList<ITemplateSelector> _templateSelectors;

        public void Initialize(string templatePath, IQuinceStore store, string defaultTemplate = "default.liquid", IList<ITemplateSelector> selectors = null )
        {
            _templatePath = templatePath;
            _defaultTemplateName = defaultTemplate;
            _parsedTemplates = new Dictionary<string, Template>();
            _namespaces = new Dictionary<string, object>
            {
                {"void", new NamespaceDrop("http://rdfs.org/ns/void#")},
                {"rdf", new NamespaceDrop("http://www.w3.org/1999/02/22-rdf-syntax-ns#")},
                {"rdfs", new NamespaceDrop("http://www.w3.org/2000/01/rdf-schema#")},
                {"xsd", new NamespaceDrop("http://www.w3.org/2001/XMLSchema#")},
                {"dcterms", new NamespaceDrop("http://purl.org/dc/terms/")},
                {"foaf", new NamespaceDrop("http://xmlns.com/foaf/0.1/")},
                {"dcat", new NamespaceDrop("http://www.w3.org/ns/dcat#") }
            };

            GetTemplate(defaultTemplate);
            Template.RegisterFilter(typeof(CollectionFilters));
            Template.RegisterFilter(typeof(StringFilters));

            _store = store;
            _templateSelectors = selectors ?? new List<ITemplateSelector>();
        }

        public string Render(Uri subjectUri, IList<Triple> triples, IList<Triple> incomingTriples, Dictionary<string, object> addVariables = null)
        {
            var templateName = DetermineTemplatePath(subjectUri, triples, _defaultTemplateName);
            var parsedTemplate = GetTemplate(templateName);
            var localVariables = Hash.FromAnonymousObject(
                new
                {
                    subject = subjectUri.ToString(),
                    triples = triples.Select(t => new TripleDrop(t, _store)).ToList(),
                    tripleCount = triples.Count,
                    incoming = incomingTriples.Select(t=>new TripleDrop(t, _store)).ToList(),
                    incomingCount = incomingTriples.Count
                });
            localVariables.Merge(_namespaces);
            if (addVariables != null) localVariables.Merge(addVariables);
            return parsedTemplate.Render(localVariables);
        }

        private string DetermineTemplatePath(Uri subjectUri, IList<Triple> triples, string defaultTemplate)
        {
            foreach (var selector in _templateSelectors)
            {
                var template = selector.SelectTemplate(subjectUri, triples);
                if (template != null) return template;
            }
            return defaultTemplate;
        }

        private Template GetTemplate(string templateName)
        {
            Template parsedTemplate;
            if (!_parsedTemplates.TryGetValue(templateName, out parsedTemplate))
            {
                var templatePath = Path.Combine(_templatePath, templateName);
                var template = File.ReadAllText(templatePath);
                parsedTemplate = Template.Parse(template);
                _parsedTemplates[templateName] = parsedTemplate;
            }
            return parsedTemplate;
        }
    }
}
