using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DataDock.Common;
using DataDock.Common.Models;
using DataDock.Worker.Liquid;
using DataDock.Worker.Templating;
using NetworkedPlanet.Quince;
using Serilog;
using VDS.RDF;

namespace DataDock.Worker
{
    public class DataDockRepository : IDataDockRepository
    {
        private readonly string _targetDirectory;
        private readonly Uri _repositoryUri;
        private readonly IQuinceStore _quinceStore;
        private readonly IProgressLog _progressLog;
        private readonly IFileGeneratorFactory _fileGeneratorFactory;
        private readonly IResourceFileMapper _rdfResourceFileMapper;
        private readonly IResourceFileMapper _htmlResourceFileMapper;
        private readonly IDataDockUriService _uriService;

        /// <summary>
        /// How many files to generate between progress reports
        /// </summary>
        private const int RdfFileGenerationReportInterval = 500;

        /// <summary>
        /// How many files to generate between progress reports
        /// </summary>
        private const int HtmlFileGenerationReportInterval = 250;

        /// <summary>
        /// Create a new repository that updates the local clone of a DataDock GitHub repository
        /// </summary>
        /// <param name="targetDirectory">The path to the directory containing the local clone</param>
        /// <param name="repositoryUri">The base IRI for DataDock graphs in this repository</param>
        /// <param name="progressLog">The progress logger to report to</param>
        /// <param name="quinceStoreFactory">a factory for creating an IQuinceStore instance to access the Quince store of the GitHub repository</param>
        /// <param name="fileFileGeneratorFactory">a factory for creating an <see cref="IFileGeneratorFactory"/> instance to generate the statically published HTML files for the GitHub repository</param>
        /// <param name="rdfResourceFileMapper">Provides the logic to map resource URIs to the path to the static RDF files for that resource</param>
        /// <param name="htmlResourceFileMapper">Provides the logic to map resource URIs to the path to the static HTML files for that resource</param>
        /// <param name="uriService">Provides the logic to generate URIs for DataDock resources</param>
        public DataDockRepository(
            string targetDirectory, 
            Uri repositoryUri, 
            IProgressLog progressLog,
            IQuinceStoreFactory quinceStoreFactory,
            IFileGeneratorFactory fileFileGeneratorFactory,
            IResourceFileMapper rdfResourceFileMapper,
            IResourceFileMapper htmlResourceFileMapper,
            IDataDockUriService uriService)
        {
            _targetDirectory = targetDirectory;
            _repositoryUri = repositoryUri;
            _progressLog = progressLog;
            _quinceStore = quinceStoreFactory.MakeQuinceStore(targetDirectory);
            _fileGeneratorFactory = fileFileGeneratorFactory;
            _rdfResourceFileMapper = rdfResourceFileMapper;
            _htmlResourceFileMapper = htmlResourceFileMapper;
            _uriService = uriService;
        }

        /// <summary>
        /// Adds content to a dataset graph in the DataDock repository, optionally overwriting existing dataset content
        /// </summary>
        /// <param name="insertTriples"></param>
        /// <param name="datasetIri"></param>
        /// <param name="dropExistingGraph"></param>
        /// <param name="metadataGraph"></param>
        /// <param name="metadataGraphIri"></param>
        /// <param name="definitionsGraph"></param>
        /// <param name="definitionsGraphIri"></param>
        /// <param name="publisherIri"></param>
        /// <param name="publisherInfo"></param>
        /// <param name="repositoryTitle"></param>
        /// <param name="repositoryDescription"></param>
        /// <param name="rootMetadataGraphIri"></param>
        public void UpdateDataset(
            IGraph insertTriples, Uri datasetIri, bool dropExistingGraph,
            IGraph metadataGraph, Uri metadataGraphIri,
            IGraph definitionsGraph, Uri definitionsGraphIri,
            Uri publisherIri, ContactInfo publisherInfo,
            string repositoryTitle, string repositoryDescription,
            Uri rootMetadataGraphIri)
        {
            try
            {
                _progressLog.Info("Updating RDF repository");

                if (dropExistingGraph)
                {
                    // Drop dataset data graph
                    _progressLog.Info("Dropping all existing RDF data in graph {0}", datasetIri);
                    _quinceStore.DropGraph(datasetIri);
                }

                // Drop dataset metadata graph
                _progressLog.Info("Dropping all metadata in graph {0}", metadataGraphIri);
                _quinceStore.DropGraph(metadataGraphIri);

                // Add triples to dataset data graph
                _progressLog.Info("Adding new RDF data to graph {0} ({1} triples)", datasetIri, insertTriples.Triples.Count);
                var addCount = 0;
                foreach (var t in insertTriples.Triples)
                {
                    _quinceStore.Assert(t.Subject, t.Predicate, t.Object, datasetIri);
                    addCount++;
                    if (addCount % 1000 == 0)
                    {
                        _progressLog.Info("Added {0} / {1} triples ({2}%)", addCount, insertTriples.Triples.Count,
                            addCount * 100 / insertTriples.Triples.Count);
                    }
                }

                // Add triples to dataset metadata graph
                _progressLog.Info("Adding dataset metadata to graph {0} ({1} triples)", metadataGraphIri, metadataGraph.Triples.Count);
                foreach (var t in metadataGraph.Triples)
                {
                    _quinceStore.Assert(t.Subject, t.Predicate, t.Object, metadataGraphIri);
                }

                // Add triples to the definitions graph
                _progressLog.Info("Adding column definition metadata to graph {0} ({1} triples)", definitionsGraphIri, definitionsGraph.Triples.Count);
                foreach (var t in definitionsGraph.Triples)
                {
                    _quinceStore.Assert(t.Subject, t.Predicate, t.Object, definitionsGraphIri);
                }

                // Update the root metadata graph to ensure it includes this dataset as a subset
                _progressLog.Info("Updating root metadata");
                var repositoryNode = insertTriples.CreateUriNode(_repositoryUri);
                var subset = insertTriples.CreateUriNode(new Uri("http://rdfs.org/ns/void#subset"));
                var rdfType = insertTriples.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
                var rdfsLabel = insertTriples.CreateUriNode(new Uri("http://www.w3.org/2000/01/rdf-schema#label"));
                var foafName = insertTriples.CreateUriNode(new Uri("http://xmlns.com/foaf/0.1/name"));
                var foafMbox = insertTriples.CreateUriNode(new Uri("http://xmlns.com/foaf/0.1/mbox"));
                var foafHomepage = insertTriples.CreateUriNode(new Uri("http://xmlns.com/foaf/0.1/homepage"));
                var dctermsPublisher = insertTriples.CreateUriNode(new Uri("http://purl.org/dc/terms/publisher"));
                var dctermsTitle = insertTriples.CreateUriNode(new Uri("http://purl.org/dc/terms/title"));
                var dctermsDescription = insertTriples.CreateUriNode(new Uri("http://purl.org/dc/terms/description"));
                var publisherNode = insertTriples.CreateUriNode(publisherIri);
                _quinceStore.Assert(repositoryNode, rdfType, insertTriples.CreateUriNode(new Uri("http://rdfs.org/ns/void#Dataset")), rootMetadataGraphIri);
                _quinceStore.Assert(repositoryNode, subset, insertTriples.CreateUriNode(datasetIri), rootMetadataGraphIri);
                _quinceStore.Assert(repositoryNode, dctermsPublisher, publisherNode, rootMetadataGraphIri);


                // Update repository title and description
                foreach (var t in _quinceStore.GetTriplesForSubject(_repositoryUri))
                {
                    if (t.GraphUri.Equals(rootMetadataGraphIri))
                    {
                        if (t.Predicate.Equals(dctermsTitle) || t.Predicate.Equals(dctermsDescription))
                        {
                            _quinceStore.Retract(t.Subject, t.Predicate, t.Object, rootMetadataGraphIri);
                        }
                    }
                }
                if (!string.IsNullOrEmpty(repositoryTitle))
                {
                    _quinceStore.Assert(repositoryNode, dctermsTitle, insertTriples.CreateLiteralNode(repositoryTitle), rootMetadataGraphIri);
                }
                if (!string.IsNullOrEmpty(repositoryDescription))
                {
                    _quinceStore.Assert(repositoryNode, dctermsDescription, insertTriples.CreateLiteralNode(repositoryDescription), rootMetadataGraphIri);
                }

                // Update publisher information
                foreach (var t in _quinceStore.GetTriplesForSubject(publisherIri))
                {
                    if (t.GraphUri.Equals(rootMetadataGraphIri))
                    {
                        _quinceStore.Retract(t.Subject, t.Predicate, t.Object, rootMetadataGraphIri);
                    }
                }
                if (!string.IsNullOrEmpty(publisherInfo?.Type))
                {
                    var publisherType = insertTriples.CreateUriNode(new Uri(publisherInfo.Type));
                    _quinceStore.Assert(publisherNode, rdfType, publisherType, rootMetadataGraphIri);
                }
                if (!string.IsNullOrEmpty(publisherInfo?.Label))
                {
                    var publisherLabel = insertTriples.CreateLiteralNode(publisherInfo.Label);
                    _quinceStore.Assert(publisherNode, rdfsLabel, publisherLabel, rootMetadataGraphIri);
                    _quinceStore.Assert(publisherNode, foafName, publisherLabel, rootMetadataGraphIri);
                }
                if (!string.IsNullOrEmpty(publisherInfo?.Email))
                {
                    _quinceStore.Assert(publisherNode, foafMbox, insertTriples.CreateLiteralNode(publisherInfo.Email), rootMetadataGraphIri);
                }
                if (!string.IsNullOrEmpty(publisherInfo?.Website))
                {
                    _quinceStore.Assert(publisherNode, foafHomepage, insertTriples.CreateUriNode(new Uri(publisherInfo.Website)), rootMetadataGraphIri);
                }

                // Flush all data to disk
                _quinceStore.Flush();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CSV conversion failed");
                throw new WorkerException(ex, "CSV conversion failed. Error updating RDF repository.");
            }

        }
        /// <summary>
        /// Remove a dataset contents and its metadata records from the repository
        /// </summary>
        /// <param name="datasetIri">The IRI of the dataset to be removed</param>
        public void DeleteDataset( Uri datasetIri)
        {
            Log.Information("DeleteDataset: {datasetIri}", datasetIri);
            var datasetMetadataGraphIri = new Uri(datasetIri + "/metadata");
            var rootMetadataGraphIri = new Uri(_repositoryUri, "metadata");

            _progressLog.Info("Dropping dataset graph {0}", datasetIri);
            _quinceStore.DropGraph(datasetIri);
            _progressLog.Info("Dropping dataset metadata graph {0}", datasetMetadataGraphIri);
            _quinceStore.DropGraph(datasetMetadataGraphIri);
            _progressLog.Info("Updating root metadata graph");
            var g = new Graph();
            var subset = g.CreateUriNode(new Uri("http://rdfs.org/ns/void#subset"));
            _quinceStore.Retract(g.CreateUriNode(_repositoryUri), subset, g.CreateUriNode(datasetIri), rootMetadataGraphIri);
            _progressLog.Info("Saving repository changes");
            _quinceStore.Flush();
        }

        /// <summary>
        /// Update or create the statically generated HTML and RDF for the DataDock repository
        /// </summary>
        /// <param name="graphFilter">An optional enumeration of the IRIs of the graphs to be published.</param>
        /// <param name="templateVariables">Additional template variables for data portal pages</param>
        public void Publish(IEnumerable<Uri> graphFilter = null, Dictionary<string, object> templateVariables = null)
        {
            GenerateRdf(graphFilter);
            GenerateHtml(templateVariables);
            GenerateVoidMetadata(templateVariables);
        }

        public void GenerateRdf(IEnumerable<Uri> graphFilter)
        {
            try
            {
                if (graphFilter == null)
                {
                    // Performing a complete reset of RDF data
                    _progressLog.Info("Performing a clean rebuild of data directory");
                    foreach (var resourcePath in _rdfResourceFileMapper.GetMappedPaths(_targetDirectory))
                    {
                        RemoveDirectory(resourcePath);
                    }
                }

                var rdfGenerator = _fileGeneratorFactory.MakeRdfFileGenerator(_rdfResourceFileMapper, graphFilter,
                    _progressLog, RdfFileGenerationReportInterval);
                _quinceStore.EnumerateSubjects(rdfGenerator);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running RDF file generation");
                throw new WorkerException(ex, "Error running RDF file generation. File generation may be incomplete.");
            }
        }


        private void GenerateHtml(Dictionary<string, object> templateVariables)
        {
            try
            {
                var templateEngine = new Liquid.LiquidViewEngine();
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var templatePath = Path.Combine(Path.GetDirectoryName(assemblyPath), "templates");
                templateEngine.Initialize(templatePath, _quinceStore,
                    selectors: new List<ITemplateSelector>
                    {
                        new RdfTypeTemplateSelector(new Uri("http://rdfs.org/ns/void#Dataset"), "dataset.liquid")
                    });

                foreach (var resourcePath in _htmlResourceFileMapper.GetMappedPaths(_targetDirectory))
                {
                    RemoveDirectory(resourcePath);
                }

                var generator = _fileGeneratorFactory.MakeHtmlFileGenerator(_uriService, _htmlResourceFileMapper, templateEngine, _progressLog, HtmlFileGenerationReportInterval, templateVariables);

                _quinceStore.EnumerateSubjects(generator);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running HTML file generation");
                throw new WorkerException(ex, "Error running HTML file generation. File generation may be incomplete.");
            }
        }

        private void GenerateVoidMetadata(Dictionary<string, object> templateVariables)
        {
            try
            {
                var templateEngine = new Liquid.LiquidViewEngine();
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var templatePath = Path.Combine(Path.GetDirectoryName(assemblyPath), "templates");
                templateEngine.Initialize(templatePath, _quinceStore, "void.liquid");
                var voidGenerator = new VoidFileGenerator(templateEngine, _quinceStore, _repositoryUri, _progressLog, templateVariables);
                var htmlPath = Path.Combine(_targetDirectory, "page", "index.html");
                var nquadsPath = Path.Combine(_targetDirectory, "data", "void.nq");
                voidGenerator.GenerateVoidHtml(htmlPath);
                voidGenerator.GenerateVoidNQuads(nquadsPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running metadata file generation");
                throw new WorkerException(ex, "Error running metadata file generation. File generation may be incomplete.");
            }
        }

        private static void RemoveDirectory(string directoryPath)
        {
            try
            {
                FileSystemHelper.DeleteDirectory(directoryPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to remove temporary directory {dirPath}", directoryPath);
            }
        }

    }
}
