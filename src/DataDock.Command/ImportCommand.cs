using DataDock.Common;
using DataDock.Worker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VDS.RDF;

namespace DataDock.Command
{
    internal class ImportCommand
    {
        private readonly ImportOptions _opts;
        private readonly IProgressLog _log;

        public ImportCommand(ImportOptions opts, IProgressLog log)
        {
            _opts = opts;
            _log = log;
        }

        public async Task<int> Run()
        {
            var baseUri = _opts.RepositoryUri.AbsoluteUri;
            if (!baseUri.EndsWith("/")) baseUri += "/";
            var metadataJson = await ParseMetadata();
            var repositoryIriService = new DataDockRepositoryUriService(_opts.RepositoryUri.AbsoluteUri);
            try
            {
                var dataGraph = await ProcessCsv(baseUri, metadataJson);
                var exampleResources = dataGraph.Triples.Select(t => t.Subject).OfType<IUriNode>().Distinct().Take(10);
                var metadataGraph = GenerateMetadataGraph(metadataJson, _opts.DownloadLinks, exampleResources);
                var definitionsGraph = new DefinitionsGraph(metadataJson).Graph;
                var datasetUri = new Uri(repositoryIriService.GetDatasetIdentifier(_opts.DatasetId));
                var metadataGraphUri = new Uri(repositoryIriService.GetDatasetMetadataIdentifier(_opts.DatasetId));
                var repository = GetDataDockRepository(); 
                repository.UpdateDataset(dataGraph, datasetUri, _opts.Overwrite,
                    metadataGraph, metadataGraphUri,
                    definitionsGraph, new Uri(repositoryIriService.DefinitionsGraphIdentifier),
                    null, null, null, null, 
                    new Uri(repositoryIriService.MetadataGraphIdentifier));
                return 0;
            }
            catch (WorkerException ex)
            {
                _log.Exception(ex, "Import failed.");
                return -1;
            }

        }

        private async Task<IGraph> ProcessCsv(string baseUri, JObject metadataJson)
        {
            var csvProcessor = new CsvProcessor(_log);
            var metadataUri = new Uri(baseUri + "/csv/" + Path.GetFileName(_opts.MetadataFile));
            var dataGraph = await csvProcessor.GenerateGraphAsync(_opts.File, _opts.MetadataFile, metadataUri, metadataJson);
            return dataGraph;
        }

        private static IGraph GenerateMetadataGraph(JObject metadataJson, IEnumerable<Uri> downloadLinks, IEnumerable<IUriNode> exampleResources)
        {
            var metadataExtractor = new MetadataExtractor(metadataJson);
            metadataExtractor.AssertModified(DateTime.UtcNow);
            metadataExtractor.AssertDataDumps(downloadLinks);
            metadataExtractor.AssertExampleResources(exampleResources);
            return metadataExtractor.Graph;
        }

        private async Task<JObject> ParseMetadata()
        {
            using var reader = new JsonTextReader(new StreamReader(_opts.MetadataFile));
            return await JObject.LoadAsync(reader);
        }

        private IDataDockRepository GetDataDockRepository()
        {
            // TODO: Currently page mapping relies on the http://datadock.io/ prefix. Make this configurable.
            var dataDockUriService = new DataDockUriService("http://datadock.io/");
            var repositoryUriService = new DataDockRepositoryUriService(_opts.RepositoryUri.AbsoluteUri);
            var resourceBaseIri = new Uri(repositoryUriService.IdentifierPrefix);
            var repoPath = _opts.RepositoryPath;
            var rdfResourceFileMapper = new ResourceFileMapper(
                new ResourceMapEntry(resourceBaseIri, Path.Combine(repoPath, "data")));
            var htmlResourceFileMapper = new ResourceFileMapper(
                new ResourceMapEntry(resourceBaseIri, Path.Combine(repoPath, "page")));
            return new DataDockRepository(_opts.RepositoryPath, _opts.RepositoryUri, _log,
                new DefaultQuinceStoreFactory(), new FileGeneratorFactory(), rdfResourceFileMapper,
                htmlResourceFileMapper, dataDockUriService);
        }
    }
}
