using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NetworkedPlanet.Quince;
using VDS.RDF;
using VDS.RDF.Writing.Formatting;

namespace DataDock.Worker
{
    public class RdfFileGenerator : ITripleCollectionHandler
    {
        private readonly IResourceFileMapper _resourceMap;
        private readonly List<Uri> _graphFilter;
        private readonly INodeFormatter _nquadsFormatter;
        private readonly IProgressLog _progressLog;
        private int _subjectCount;
        private readonly bool _noFilter;
        private readonly int _reportInterval;

        public RdfFileGenerator(IResourceFileMapper resourceMap, IEnumerable<Uri> graphFilter, IProgressLog progressLog, int reportInterval)
        {
            _resourceMap = resourceMap;
            _graphFilter = graphFilter?.ToList() ?? new List<Uri>(0);
            _noFilter = _graphFilter.Count == 0;
            _nquadsFormatter = new NQuads11Formatter();
            _progressLog = progressLog;
            _reportInterval = reportInterval;
        }

        public bool HandleTripleCollection(IList<Triple> tripleCollection)
        {
            var subject = (tripleCollection[0].Subject as IUriNode)?.Uri;
            try
            {
                if (_resourceMap.CanMap(subject))
                {
                    if (_noFilter || tripleCollection.Any(t => _graphFilter.Contains(t.GraphUri)))
                    {
                        // There is some data in the graph that was updated, so we should regenerate the files for this resource
                        var targetPath = _resourceMap.GetPathFor(subject);
                        var targetDir = Path.GetDirectoryName(targetPath);
                        if (targetDir != null && !Directory.Exists(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }
                        var graph = new Graph();
                        graph.Assert(tripleCollection);

#if GENERATE_RDFXML
                        GenerateRdfXmlFile(subject, targetPath, graph);
#endif

                        GenerateNQuadsFile(subject, targetPath, graph);
                    }
                }
                _subjectCount++;
                if (_subjectCount % _reportInterval == 0)
                {
                    _progressLog.Info("Generating static files - {0} subjects processed.", _subjectCount);
                }
            }
            catch (Exception ex)
            {
                _progressLog.Exception(ex, "Error generating static files for subject {0}: {1}", subject, ex.Message);
            }
            return true;

        }

        private void GenerateNQuadsFile(Uri subject, string targetPath, IGraph graph)
        {
            try
            {
                var nquadsTargetPath = targetPath + ".nq";
                using (var output = File.Open(nquadsTargetPath, FileMode.Create))
                {
                    using (var writer = new StreamWriter(output, encoding: Encoding.UTF8))
                    {
                        foreach (var t in graph.Triples)
                        {
                            writer.WriteLine(FormatQuad(_nquadsFormatter, t));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _progressLog.Exception(ex, "Error generating NQuads file for subject {0}: {1}", subject, ex.Message);
            }
        }

#if GENERATE_RDFXML
        private void GenerateRdfXmlFile( Uri subject, string targetPath, IGraph graph)
        {
            try
            {
                var rdfXmlTargetPath = targetPath + ".rdf";
                var rdfWriter = new RdfXmlWriter();
                rdfWriter.Save(graph, rdfXmlTargetPath);
            }
            catch (Exception ex)
            {
                _progressLog.Exception(ex, "Error generating RDF/XML file for subject {0}: {1}", subject, ex.Message);
            }
        }
#endif


        private static string FormatQuad(INodeFormatter formatter, Triple t)
        {
            var line = new StringBuilder();
            line.Append(formatter.Format(t.Subject));
            line.Append(' ');
            line.Append(formatter.Format(t.Predicate));
            line.Append(' ');
            line.Append(formatter.Format(t.Object));
            line.Append(" <");
            line.Append(t.GraphUri);
            line.Append(">.");
            return line.ToString();
        }
    }
}