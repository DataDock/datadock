using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetworkedPlanet.Quince;
using VDS.RDF;
using VDS.RDF.Writing.Formatting;

namespace DataDock.Worker
{
    public class VoidFileGenerator
    {
        private readonly IViewEngine _viewEngine;
        private readonly IQuinceStore _quinceStore;
        private readonly Uri _repositoryUri;
        private readonly IProgressLog _progressLog;
        private readonly IGraph _graph;
        private readonly IUriNode _voidSubset;
        private IUriNode _dctermsPublisher;
        private readonly Dictionary<string, object> _addVariables;

        public VoidFileGenerator(IViewEngine viewEngine, IQuinceStore quinceStore, Uri repositoryUri, IProgressLog progressLog, Dictionary<string, object> addVariables)
        {
            _viewEngine = viewEngine;
            _quinceStore = quinceStore;
            _repositoryUri = repositoryUri;
            _progressLog = progressLog;
            _graph = new Graph();
            _voidSubset = _graph.CreateUriNode(new Uri("http://rdfs.org/ns/void#subset"));
            _dctermsPublisher = _graph.CreateUriNode(new Uri("http://purl.org/dc/terms/publisher"));
            _addVariables = addVariables ?? new Dictionary<string, object>();
        }

        public void GenerateVoidHtml(string targetFileName)
        {
            var rootTripleCollection = _quinceStore.GetTriplesForSubject(_graph.CreateUriNode(_repositoryUri)).ToList();
            try
            {
                var voidLink = new Uri(_repositoryUri, "data/void.nq");
                _addVariables.Remove("nquads");
                _addVariables.Add("nquads", voidLink.ToString());
                _addVariables.Remove("htmlPath");
                _addVariables.Add("htmlPath", targetFileName);

                var voidHtml = _viewEngine.Render(_repositoryUri, rootTripleCollection, new List<Triple>(),
                    _addVariables);
                using (var output = File.Open(targetFileName, FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(output))
                    {
                        writer.Write(voidHtml);
                    }
                }
            }
            catch (Exception ex)
            {
                _progressLog.Exception(ex, "Error writing dataset metadata HTML. Cause: {0}", ex.Message);
            }
        }

        public void GenerateVoidNQuads(string targetFileName)
        {
            var targetDir = Path.GetDirectoryName(targetFileName);
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            var rootTripleCollection = _quinceStore.GetTriplesForSubject(_graph.CreateUriNode(_repositoryUri)).ToList();
            foreach (var subsetTriple in rootTripleCollection.WithPredicate(_voidSubset).ToList())
            {
                rootTripleCollection.AddRange(_quinceStore.GetTriplesForSubject(subsetTriple.Object).Where(t => !rootTripleCollection.Contains(t)));
            }
            foreach (var publisherTriple in rootTripleCollection.WithPredicate(_dctermsPublisher).ToList())
            {
                rootTripleCollection.AddRange(_quinceStore.GetTriplesForSubject(publisherTriple.Object).Where(t => !rootTripleCollection.Contains(t)));
            }
            var formatter = new NQuads11Formatter();
            using (var output = File.Open(targetFileName, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(output))
                {
                    foreach (var t in rootTripleCollection)
                    {
                        writer.WriteLine("{0} {1} {2} {3} .",
                            formatter.Format(t.Subject), formatter.Format(t.Predicate), formatter.Format(t.Object), formatter.FormatUri(t.GraphUri));
                    }
                }
            }
        }
    }
}
