using System;
using Newtonsoft.Json.Linq;
using VDS.RDF;

namespace DataDock.Worker
{
    public class DefinitionsGraph
    {
        public IGraph Graph { get; }
        private const string Rdfs = "http://www.w3.org/2000/01/rdf-schema#";
        private const string RdfsLabel = Rdfs + "label";

        public DefinitionsGraph(JObject metadataJson)
        {
            Graph = new Graph();
            GenerateColumnDefinitions(metadataJson);
        }

        private void GenerateColumnDefinitions(JObject metadataJson)
        {
            JToken tok;
            IUriNode rdfsLabel = Graph.CreateUriNode(new Uri(RdfsLabel));
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
                                            Graph.CreateUriNode(new Uri(propertyUrl.Value<string>()));
                                        if (titles != null)
                                        {
                                            JsonHelper.MakeTriples(propNode, rdfsLabel, titles, Graph);
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
