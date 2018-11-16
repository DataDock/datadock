using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using VDS.RDF;

namespace DataDock.Worker
{
    // TODO: It would be better to simply do standard JSON-LD processing and then drop triples with predicates in the CSVW namespace - requires some JSON-LD support in dotNetRDF (issue #84).
    /// <summary>
    /// 
    /// </summary>
    public class MetdataExtractor
    {
        private const string XmlSchema = "http://www.w3.org/2001/XMLSchema#";
        private const string Rdfs = "http://www.w3.org/2000/01/rdf-schema#";
        private const string RdfsLabel = Rdfs + "label";
        private readonly Dictionary<string, string> _supportedPrefixes = new Dictionary<string, string>
        {
            {"cc", "http://creativecommons.org/ns#" },
            {"dc", "http://purl.org/dc/terms/"},
            {"dcat", "http://www.w3.org/ns/dcat#"},
            {"foaf", "http://xmlns.com/foaf/0.1/" },
            {"void", "http://rdfs.org/ns/void#"},
            {"xsd", "http://www.w3.org/2001/XMLSchema#" }
        };

        public void Run(JObject metadataObject, Graph metadataGraph, Uri publisherUri, int dataGraphTripleCount, DateTime? modifiedDateTime)
        {
            var urlValue = metadataObject.GetValue("url") as JValue;
            var datasetUrl = urlValue?.Value<string>();
            if (datasetUrl == null) throw new Exception("No url property found in metadata");

            var subject = metadataGraph.CreateUriNode(new Uri(datasetUrl));
            foreach (var p in metadataObject.Properties().Where(p => p.Name.Contains(":")))
            {
                var ix = p.Name.IndexOf(":");
                var prefix = p.Name.Substring(0, ix);
                var rest = p.Name.Substring(ix + 1);
                if (_supportedPrefixes.ContainsKey(prefix))
                {
                    var predicate = metadataGraph.CreateUriNode(new Uri(_supportedPrefixes[prefix] + rest));
                    foreach (var value in p)
                    {
                        MakeTriples(subject, predicate, value, metadataGraph);
                    }
                }
            }
            // Also assert that this is a void:Dataset
            metadataGraph.Assert(subject,
                metadataGraph.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")),
                metadataGraph.CreateUriNode(new Uri("http://rdfs.org/ns/void#Dataset")));

            // Assert publisher data
            metadataGraph.Assert(subject, metadataGraph.CreateUriNode(new Uri("http://purl.org/dc/terms/publisher")),
                metadataGraph.CreateUriNode(publisherUri));

            // Assert triple count
            metadataGraph.Assert(subject,
                metadataGraph.CreateUriNode(new Uri("http://rdfs.org/ns/void#triples")),
                metadataGraph.CreateLiteralNode(dataGraphTripleCount.ToString("D"),
                    new Uri("http://www.w3.org/2001/XMLSchema#integer")));

            if (modifiedDateTime.HasValue)
            {
                // Assert modified date/time
                if (modifiedDateTime.Value.Kind != DateTimeKind.Utc)
                {
                    modifiedDateTime = modifiedDateTime.Value.ToUniversalTime();
                }
                metadataGraph.Assert(subject,
                    metadataGraph.CreateUriNode(new Uri("http://purl.org/dc/terms/modified")),
                    metadataGraph.CreateLiteralNode(modifiedDateTime.Value.ToString("yyyy-MM-dd"),
                        new Uri("http://www.w3.org/2001/XMLSchema#date")));
            }
        }

        private static void MakeTriples(INode subject, INode predicate, JToken value, IGraph graph)
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
                                graph.CreateLiteralNode(jvalue.Value<string>(), new Uri(XmlSchema + "string")));
                        }
                        break;
                    case JTokenType.Date:
                        graph.Assert(subject, predicate,
                            graph.CreateLiteralNode(jvalue.Value<DateTime>().ToString("O"),
                                new Uri(XmlSchema + "dateTime")));
                        break;
                    case JTokenType.Integer:
                        graph.Assert(subject, predicate,
                            graph.CreateLiteralNode(jvalue.Value<long>().ToString("D"), new Uri(XmlSchema + "integer")));
                        break;
                }
            }
        }

        public void GenerateColumnDefinitions(JObject metadataJson, IGraph definitionsGraph)
        {
            JToken tok;
            if (metadataJson.TryGetValue("tableSchema", out tok))
            {
                var tableSchema = tok as JObject;
                if (tableSchema != null)
                {
                    if (tableSchema.TryGetValue("columns", out tok))
                    {
                        var columns = tok as JArray;
                        if (columns != null)
                        {
                            foreach (var col in columns)
                            {
                                var colObject = col as JObject;
                                if (colObject != null)
                                {
                                    var propertyUrl = col["propertyUrl"] as JValue;
                                    var titles = col["titles"] as JArray;
                                    if (propertyUrl != null)
                                    {
                                        var propNode =
                                            definitionsGraph.CreateUriNode(new Uri(propertyUrl.Value<string>()));
                                        if (titles != null)
                                        {
                                            var rdfsLabel = definitionsGraph.CreateUriNode(new Uri(RdfsLabel));
                                            MakeTriples(propNode, rdfsLabel, titles, definitionsGraph);
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }
    }
}