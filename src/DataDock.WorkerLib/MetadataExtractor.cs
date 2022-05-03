using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace DataDock.Worker
{
    // TODO: It would be better to simply do standard JSON-LD processing and then drop triples with predicates in the CSVW namespace - requires some JSON-LD support in dotNetRDF (issue #84).
    /// <summary>
    /// 
    /// </summary>
    public class MetadataExtractor
    {
        private readonly Dictionary<string, string> _supportedPrefixes = new Dictionary<string, string>
        {
            {"cc", "http://creativecommons.org/ns#" },
            {"dc", "http://purl.org/dc/terms/"},
            {"dcat", "http://www.w3.org/ns/dcat#"},
            {"foaf", "http://xmlns.com/foaf/0.1/" },
            {"void", "http://rdfs.org/ns/void#"},
            {"xsd", "http://www.w3.org/2001/XMLSchema#" }
        };

        private readonly IUriNode _subject;

        public MetadataExtractor(JObject metadataObject)
        {
            Graph = new Graph();
            _subject = ExtractCsvMetadata(metadataObject);
        }

        public IGraph Graph { get; }

        private IUriNode ExtractCsvMetadata(JObject metadataObject)
        {
            var urlValue = metadataObject.GetValue("url") as JValue;
            var datasetUrl = urlValue?.Value<string>();
            if (datasetUrl == null)
            {
                throw new Exception("No url property found in metadata");
            }

            var subject = Graph.CreateUriNode(new Uri(datasetUrl));
            foreach (var p in metadataObject.Properties().Where(p => p.Name.Contains(":")))
            {
                var ix = p.Name.IndexOf(":");
                var prefix = p.Name.Substring(0, ix);
                var rest = p.Name.Substring(ix + 1);
                if (_supportedPrefixes.ContainsKey(prefix))
                {
                    var predicate = Graph.CreateUriNode(new Uri(_supportedPrefixes[prefix] + rest));
                    foreach (var value in p)
                    {
                        JsonHelper.MakeTriples(subject, predicate, value, Graph);
                    }
                }
            }
            // Also assert that this is a void:Dataset
            Graph.Assert(subject,
                Graph.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")),
                Graph.CreateUriNode(new Uri("http://rdfs.org/ns/void#Dataset")));

            return subject;
        }

        public void AssertPublisher(Uri publisherUri)
        {
            if (publisherUri != null)
            {
                // Assert publisher
                Graph.Assert(_subject,
                    Graph.CreateUriNode(new Uri("http://purl.org/dc/terms/publisher")),
                    Graph.CreateUriNode(publisherUri));
            }
        }

        public void AssertTripleCount(int tripleCount)
        {
            Graph.Assert(_subject,
                Graph.CreateUriNode(new Uri("http://rdfs.org/ns/void#triples")),
                Graph.CreateLiteralNode(tripleCount.ToString("D"),
                    new Uri("http://www.w3.org/2001/XMLSchema#integer")));
        }

        public void AssertModified(DateTime modifiedDateTime)
        {
            if (modifiedDateTime.Kind != DateTimeKind.Utc)
            {
                modifiedDateTime = modifiedDateTime.ToUniversalTime();
            }

            Graph.Assert(_subject,
                Graph.CreateUriNode(new Uri("http://purl.org/dc/terms/modified")),
                Graph.CreateLiteralNode(modifiedDateTime.ToString("yyyy-MM-dd"),
                    new Uri("http://www.w3.org/2001/XMLSchema#date")));
        }

        public void AssertDataDumps(IEnumerable<Uri> dataDumps)
        {
            var ddNode = Graph.CreateUriNode(new Uri("http://rdfs.org/ns/void#dataDump"));
            foreach (var downloadUri in dataDumps)
            {
                Graph.Assert(_subject, ddNode, Graph.CreateUriNode(downloadUri));
            }
        }

        public void AssertExampleResources(IEnumerable<IUriNode> resourceNodes)
        {
            var exampleResource = Graph.CreateUriNode(new Uri("http://rdfs.org/ns/void#exampleResource"));
            foreach (var resourceNode in resourceNodes)
            {
                Graph.Assert(_subject, exampleResource, resourceNode);
            }
        }

        


    }

    internal static class JsonHelper
    {
        public static void MakeTriples(INode subject, INode predicate, JToken value, IGraph graph)
        {
            var array = value as JArray;
            if (array != null)
            {
                foreach (var item in array)
                {
                    MakeTriples(subject, predicate, item, graph);
                }
                return;
            }
            var jvalue = value as JValue;
            if (jvalue != null)
            {
                switch (jvalue.Type)
                {
                    case JTokenType.String:
                        var stringValue = jvalue.Value<string>()?.Trim();
                        if (string.IsNullOrEmpty(stringValue)) break;
                        Uri uri;
                        //KA: Limit the IRIs to http:// and https:// to avoid treating text that starts with some word and a colon being treated as an IRI
                        if (!string.IsNullOrWhiteSpace(stringValue) &&
                            (stringValue.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || stringValue.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)) &&
                            Uri.TryCreate(stringValue, UriKind.Absolute, out uri))
                        {
                            graph.Assert(subject, predicate, graph.CreateUriNode(uri));
                        }
                        else
                        {
                            graph.Assert(subject, predicate,
                                graph.CreateLiteralNode(jvalue.Value<string>(), new Uri(XmlSpecsHelper.XmlSchemaDataTypeString)));
                        }
                        break;
                    case JTokenType.Date:
                        graph.Assert(subject, predicate,
                            graph.CreateLiteralNode(jvalue.Value<DateTime>().ToString("O"),
                                new Uri(XmlSpecsHelper.XmlSchemaDataTypeDateTime)));
                        break;
                    case JTokenType.Integer:
                        graph.Assert(subject, predicate,
                            graph.CreateLiteralNode(jvalue.Value<long>().ToString("D"), new Uri(XmlSpecsHelper.XmlSchemaDataTypeInteger)));
                        break;
                }
            }
        }
    }
}