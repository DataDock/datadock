using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DataDock.Web.Services;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NetworkedPlanet.Quince;
using VDS.Common.Tries;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Parsing.Tokens;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;
using Graph = VDS.RDF.Graph;
using NQuadsParser = VDS.RDF.Parsing.NQuadsParser;

namespace DataDock.Web.Controllers
{
    [Route("query/{owner}/{repo}")]
    [Produces("application/n-quads")]
    [ApiController]
    public class QueryController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly MD5 _hasher;

        public QueryController(DirectoryMapCache dirMapCache)
        {
            _cache = dirMapCache.Cache;
            _hasher = MD5.Create();
        }

        [HttpGet]
        public async Task<IActionResult> GetLinkedDataFragmentAsync(
            [FromRoute] string owner,
            [FromRoute] string repo,
            [FromQuery] string s = null,
            [FromQuery] string p = null,
            [FromQuery] string o = null)
        {
            var requestBaseUri = new Uri(Request.GetUri(), ".");
            var resultGraph = new Graph {BaseUri = requestBaseUri};
            var resultStore = new TripleStore();
            resultStore.Add(resultGraph);

            var gitHubIri = $"https://{owner}.github.io/{repo}/quince/dirmap.txt";
            if (s != null || p != null || o != null)
            {
                var directoryMap = await GetDirectoryMapAsync(gitHubIri);
                string ixPath = null;
                if (s != null)
                {
                    ixPath = "_s" + HashString(s);
                } else if (o != null)
                {
                    ixPath = "_o" + HashString(o);
                }
                else if (p != null)
                {
                    ixPath = "_p" + HashString(p);
                }

                var key = PathToKey(ixPath);
                var trieNode = directoryMap.FindPredecessor(key);
                string filePath;
                if (trieNode == null)
                {
                    filePath = ixPath.Substring(0, 2) + ".nq";
                }
                else
                {
                    var dirPath = GetTriePath(trieNode);
                    filePath = dirPath + key.Substring(dirPath.Length, 2) + ".nq";
                }

                await GetQuads(owner, repo, filePath, MakeFilter(resultStore, s, p , o));
                AddControlQuads(Request.GetUri(), requestBaseUri, resultStore);
                return new OkObjectResult(resultStore);
            }

            return BadRequest();
        }

        private async Task GetQuads(string owner, string repo, string filePath, IRdfHandler rdfHandler)
        {
            var fileUri = $"https://{owner}.github.io/{repo}/quince/{filePath}";
            using (var client = new HttpClient())
            {
                var content = await client.GetStringAsync(fileUri);
                var parser = new NQuadsParser(NQuadsSyntax.Rdf11);
                using (var reader = new StringReader(content))
                {
                    parser.Load(rdfHandler, reader);
                }
            }
        }

        private void AddControlQuads(Uri requestUri, Uri requestBaseUri, ITripleStore resultStore)
        {
            var metadataGraphUri = UriFactory.Create(requestUri + "#metadata");
            var g = new Graph {BaseUri = metadataGraphUri};
            var resultTripleCount = resultStore.Triples.Count();
            g.NamespaceMap.AddNamespace("foaf", UriFactory.Create("http://xmlns.com/foaf/0.1/"));
            g.NamespaceMap.AddNamespace("hydra", UriFactory.Create("http://www.w3.org/ns/hydra/core#"));
            g.NamespaceMap.AddNamespace("void", UriFactory.Create("http://rdfs.org/ns/void#"));
            g.NamespaceMap.AddNamespace("rdf", UriFactory.Create("http://www.w3.org/1999/02/22-rdf-syntax-ns#subject"));
            var metadataGraphNode = g.CreateUriNode(metadataGraphUri);
            var datasetNode = g.CreateUriNode(UriFactory.Create(requestBaseUri + "#dataset"));
            var requestNode = g.CreateUriNode(requestUri);
            var tripleCountNode = g.CreateLiteralNode(resultTripleCount.ToString(CultureInfo.InvariantCulture),
                UriFactory.Create("http://www.w3.org/2001/XMLSchema#integer"));
            g.Assert(metadataGraphNode, g.CreateUriNode("foaf:primaryTopic"), g.CreateUriNode(requestUri));
            g.Assert(requestNode, g.CreateUriNode("void:triples"),tripleCountNode);
            g.Assert(requestNode, g.CreateUriNode("hydra:totalItems"), tripleCountNode);
            g.Assert(datasetNode, g.CreateUriNode("void:subset"), requestNode);
            var searchNode = g.CreateBlankNode("search");
            var subjectMappingNode = g.CreateBlankNode("smap");
            var predicateMappingNode = g.CreateBlankNode("pmap");
            var objectMappingNode = g.CreateBlankNode("omap");
            var searchTemplate = requestBaseUri + "{?s,p,o}";
            var hydraMapping = g.CreateUriNode("hydra:mapping");
            var hydraVariable = g.CreateUriNode("hydra:variable");
            var hydraProperty = g.CreateUriNode("hydra:property");

            g.Assert(datasetNode, g.CreateUriNode("hydra:search"), searchNode);
            g.Assert(searchNode, g.CreateUriNode("hydra:template"), g.CreateLiteralNode(searchTemplate));
            g.Assert(searchNode, hydraMapping, subjectMappingNode);
            g.Assert(searchNode, hydraMapping, predicateMappingNode);
            g.Assert(searchNode, hydraMapping, objectMappingNode);
            g.Assert(subjectMappingNode, hydraVariable, g.CreateLiteralNode("s"));
            g.Assert(subjectMappingNode, hydraProperty, g.CreateUriNode("rdf:subject"));
            g.Assert(predicateMappingNode, hydraVariable, g.CreateLiteralNode("o"));
            g.Assert(predicateMappingNode, hydraProperty, g.CreateUriNode("rdf:predicate"));
            g.Assert(objectMappingNode, hydraVariable, g.CreateLiteralNode("p"));
            g.Assert(objectMappingNode, hydraProperty, g.CreateUriNode("rdf:object"));

            resultStore.Add(g);
        }

        private string GetTriePath(ITrieNode<string, string> node)
        {
            if (node.Parent == null)
            {
                return node.KeyBit;
            }
            var pathBuilder = new StringBuilder();
            GetTriePath(node.Parent, pathBuilder);
            pathBuilder.Append(node.KeyBit);
            pathBuilder.Append("/");
            return pathBuilder.ToString();
        }

        private void GetTriePath(ITrieNode<string, string> node, StringBuilder builder)
        {
            if (!node.IsRoot)
            {
                GetTriePath(node.Parent, builder);
                builder.Append('/');
                builder.Append(node.KeyBit);
            }
        }

        private async Task<Trie<string, string, string>> GetDirectoryMapAsync(string gitHubIri)
        {
            _cache.TryGetValue(gitHubIri, out var cacheEntry);
            if (cacheEntry is Trie<string, string, string> dirMap)
            {
                return dirMap;
            }

            using (var c = new HttpClient())
            {
                var dirMapText = await c.GetStringAsync(gitHubIri);
                dirMap = new Trie<string, string, string>(SplitPath);
                foreach (var line in dirMapText.Split("\n"))
                {
                    dirMap.Add(line, string.Empty);
                }

                _cache.CreateEntry(gitHubIri)
                    .SetSize(dirMapText.Length)
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            }

            return dirMap;
        }

        private static IEnumerable<string> SplitPath(string path)
        {
            return path.Split('/');
        }

        private static string PathToKey(string path)
        {
            var key = new StringBuilder(path.Substring(0, 2));
            for (var i = 2; i < path.Length; i += 2)
            {
                key.Append("/");
                key.Append(path.Substring(i, 2));
            }

            return key.ToString();
        }

        private string HashString(string str)
        {
            if (!str.StartsWith('"'))
            {
                if (str.Contains("://"))
                {
                    str = $"<{str}>";
                }
            }

            return _hasher.ComputeHash(Encoding.UTF8.GetBytes(str)).ToHexString();
        }

        private FilterHandler MakeFilter(ITripleStore store, string s, string p, string o)
        {
            var g = store.Graphs.First();
            var subjectFilter = ParseToken(s, g);
            var predicateFilter = ParseToken(p, g);
            var objectFilter = ParseToken(o, g);
            return new FilterHandler(new StoreHandler(store), subjectFilter, predicateFilter, objectFilter, null);
        }

        private static INode ParseToken(string tokenString, IGraph g)
        {
            if (string.IsNullOrEmpty(tokenString)) return null;
            if (tokenString.StartsWith("?")) return g.CreateVariableNode(tokenString.Substring(1));
            if (!tokenString.StartsWith("\"")) tokenString = "<" + tokenString + ">";
            using (var reader = new StringReader(tokenString + "."))
            {
                var tokenizer = new NTriplesTokeniser(reader, NTriplesSyntax.Rdf11);
                var token = tokenizer.GetNextToken();
                if (token.TokenType == Token.BOF) token = tokenizer.GetNextToken();
                switch (token.TokenType)
                {
                    case Token.URI:
                        return g.CreateUriNode(new Uri(token.Value));
                    case Token.LITERAL:
                        var litValue = token.Value;
                        Uri dt = null;
                        string langSpec = null;
                        var t = tokenizer.GetNextToken();
                        while (t.TokenType != Token.DOT)
                        {
                            if (t.TokenType == Token.URI)
                            {
                                dt = new Uri(t.Value);
                            } else if (t.TokenType == Token.LANGSPEC)
                            {
                                langSpec = t.Value;
                            }
                            t = tokenizer.GetNextToken();
                        }

                        if (dt != null) return g.CreateLiteralNode(litValue, dt);
                        if (langSpec != null) return g.CreateLiteralNode(litValue, langSpec);
                        return g.CreateLiteralNode(litValue);
                }
            }

            return null;
        }
    }

    public class FilterHandler : BaseRdfHandler, IWrappingRdfHandler
    {
        private IRdfHandler _handler;
        private INode _subjectFilter;
        private INode _predicateFilter;
        private INode _objectFilter;
        private INode _graphFilter;
        private Dictionary<string, INode> _variableMap;


        public FilterHandler(IRdfHandler baseHandler, INode subjectFilter, INode predicateFilter, INode objectFilter,
            INode graphFilter)
        {
            _handler = baseHandler;
            _subjectFilter = subjectFilter;
            _predicateFilter = predicateFilter;
            _objectFilter = objectFilter;
            _graphFilter = graphFilter;
            _variableMap = new Dictionary<string, INode>();
        }

        public IEnumerable<IRdfHandler> InnerHandlers => _handler.AsEnumerable();

        protected override void StartRdfInternal()
        {
            base.StartRdfInternal();
            _handler.StartRdf();
        }

        protected override void EndRdfInternal(bool ok)
        {
            _handler.EndRdf(ok);
            base.EndRdfInternal(ok);
        }

        protected override bool HandleBaseUriInternal(Uri baseUri)
        {
            return _handler.HandleBaseUri(baseUri);
        }

        protected override bool HandleNamespaceInternal(string prefix, Uri namespaceUri)
        {
            return _handler.HandleNamespace(prefix, namespaceUri);
        }

        protected override bool HandleTripleInternal(Triple t)
        {
            _variableMap.Clear();
            if ((_subjectFilter != null) && !IsMatch(t.Subject, _subjectFilter, _variableMap)) return true;
            if (_predicateFilter != null && !IsMatch(t.Predicate, _predicateFilter, _variableMap)) return true;
            if (_objectFilter != null && !IsMatch(t.Object, _objectFilter, _variableMap)) return true;
            if (_graphFilter != null && !IsMatch(t.GraphUri, _graphFilter, _variableMap)) return true;
            return _handler.HandleTriple(t);
        }

        protected bool IsMatch(INode node, INode filter, Dictionary<string, INode> variableMap)
        {
            switch (filter)
            {
                case null:
                    return true;
                case IVariableNode varNode when variableMap.ContainsKey(varNode.VariableName):
                    return IsMatch(node, variableMap[varNode.VariableName], variableMap);
                case IVariableNode varNode:
                    variableMap.Add(varNode.VariableName, node);
                    return true;
                default:
                    return node.Equals(filter);
            }
        }

        protected bool IsMatch(Uri uri, INode filter, Dictionary<string, INode> varibleMap)
        {
            switch (filter)
            {
                case null:
                    return true;
                case IVariableNode varNode when _variableMap.ContainsKey(varNode.VariableName):
                    return IsMatch(uri, varibleMap[varNode.VariableName], varibleMap);
                case IVariableNode varNode:
                    return true;
                case IUriNode uriNode:
                    return uriNode.Uri.Equals(uri);
                default:
                    return false;
            }
        }

        public override bool AcceptsAll => true;
    }
}