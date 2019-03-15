using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Common;
using DataDock.CsvWeb;
using DataDock.CsvWeb.Metadata;
using DataDock.CsvWeb.Parsing;
using DataDock.CsvWeb.Rdf;
using Newtonsoft.Json.Linq;
using Serilog;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using FileMode = System.IO.FileMode;

namespace DataDock.Worker.Processors
{
    public class ImportJobProcessor : PublishingJobProcessor, IProgress<int>
    {
        private readonly WorkerConfiguration _configuration;
        private readonly GitCommandProcessor _git;
        private readonly IDatasetStore _datasetStore;
        private readonly IFileStore _jobFileStore;
        private readonly IDataDockRepositoryFactory _dataDataDockRepositoryFactory;
        private readonly IDataDockUriService _dataDockUriService;
        private const int CsvConversionReportInterval = 250;

        public ImportJobProcessor(
            WorkerConfiguration configuration,
            GitCommandProcessor gitProcessor,
            IGitHubClientFactory gitHubClientFactory,
            IDatasetStore datasetStore,
            IFileStore jobFileStore,
            IOwnerSettingsStore ownerSettingsStore,
            IRepoSettingsStore repoSettingsStore,
            IDataDockRepositoryFactory dataDockRepositoryFactory,
            IDataDockUriService dataDockUriService) : base(configuration, ownerSettingsStore, repoSettingsStore, gitHubClientFactory)
        {
            _configuration = configuration;
            _git = gitProcessor;
            _datasetStore = datasetStore;
            _jobFileStore = jobFileStore;
            _dataDataDockRepositoryFactory = dataDockRepositoryFactory;
            _dataDockUriService = dataDockUriService;
        }

        protected override async Task RunJob(JobInfo job, UserAccount userAccount)
        {
            ProgressLog.Info("Starting import job processing for " + userAccount.UserId);

            var targetDirectory = Path.Combine(_configuration.RepoBaseDir, job.JobId);
            Log.Information("Using local directory {localDirPath}", targetDirectory);

            // Clone the repository
            await _git.CloneRepository(job.GitRepositoryUrl, targetDirectory, AuthenticationToken, userAccount);

            // Retrieve CSV and CSVM files to src directory in the repository
            await AddCsvFilesToRepository(targetDirectory,
                job.DatasetId,
                job.CsvFileName,
                job.CsvFileId,
                job.CsvmFileId);

            var csvPath = Path.Combine(targetDirectory, "csv", job.DatasetId, job.CsvFileName);
            var metaPath = Path.Combine(targetDirectory, "csv", job.DatasetId, job.CsvFileName + "-metadata.json");

            // Parse the JSON metadata
            JObject metadataJson;
            using (var metadataReader = File.OpenText(metaPath))
            {
                var metadataString = metadataReader.ReadToEnd();
                metadataJson = JObject.Parse(metadataString);
            }

            // Run the CSV to RDF conversion
            var repositoryUri = new Uri(_dataDockUriService.GetRepositoryUri(job.OwnerId, job.RepositoryId));
            var publisherIri = new Uri(_dataDockUriService.GetRepositoryPublisherIdentifier(job.OwnerId, job.RepositoryId));
            var datasetUri = new Uri(job.DatasetIri);
            var datasetMetadataGraphIri = new Uri(datasetUri + "/metadata");
            var rootMetadataGraphIri = new Uri(_dataDockUriService.GetMetadataGraphIdentifier(job.OwnerId, job.RepositoryId));
            var definitionsGraphIri = new Uri(_dataDockUriService.GetDefinitionsGraphIdentifier(job.OwnerId, job.RepositoryId));
            var dateTag = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var releaseTag = MakeSafeTag(job.DatasetId + "_" + dateTag);
            var publisher = await GetPublisherContactInfo(job.OwnerId, job.RepositoryId);
            var ntriplesDownloadLink =
                new Uri($"https://github.com/{job.OwnerId}/{job.RepositoryId}/releases/download/{releaseTag}/{releaseTag}.nt.gz");
            var csvDownloadLink =
                new Uri(repositoryUri + $"csv/{job.DatasetId}/{job.CsvFileName}");

            IGraph datasetGraph;
            using (var tmpReader = File.OpenText(csvPath))
            {
                var header = tmpReader.ReadLine();
                Log.Information("CSV header: {CsvHeader}",header);
            }

            var metadataBaseUri = new Uri(datasetUri + "/csv/" + job.CsvFileName + "-metadata.json");
            datasetGraph = await GenerateDatasetGraphAsync(csvPath, metadataJson, metadataBaseUri);
            IGraph metadataGraph = GenerateMetadataGraph(datasetUri, publisherIri, metadataJson,
                new[] { ntriplesDownloadLink, csvDownloadLink }, datasetGraph);

            IGraph definitionsGraph = GenerateDefinitionsGraph(metadataJson);

            

            var dataDataDockRepository = _dataDataDockRepositoryFactory.GetRepositoryForJob(job, ProgressLog);
            dataDataDockRepository.UpdateDataset(
                datasetGraph, datasetUri, job.OverwriteExistingData,
                metadataGraph, datasetMetadataGraphIri, 
                definitionsGraph, definitionsGraphIri, 
                publisherIri, publisher,
                "", "",
                rootMetadataGraphIri);

            await UpdateHtmlPagesAsync(dataDataDockRepository,
                new[] {datasetUri, datasetMetadataGraphIri, rootMetadataGraphIri});

            // Add and Commit all changes
            if (await _git.CommitChanges(targetDirectory,
                $"Added {job.CsvFileName} to dataset {job.DatasetIri}", userAccount))
            {
                await _git.PushChanges(job.GitRepositoryUrl, targetDirectory, AuthenticationToken);
                await _git.MakeRelease(datasetGraph, releaseTag, job.OwnerId, job.RepositoryId, job.DatasetId, targetDirectory, AuthenticationToken);
            }

            // Update the dataset repository
            try
            {
                var voidMetadataJson = ExtractVoidMetadata(metadataGraph);
                var datasetInfo = new DatasetInfo
                {
                    OwnerId = job.OwnerId,
                    RepositoryId = job.RepositoryId,
                    DatasetId = job.DatasetId,
                    LastModified = DateTime.UtcNow,
                    CsvwMetadata = metadataJson,
                    VoidMetadata = voidMetadataJson,
                    ShowOnHomePage = job.IsPublic,
                    Tags = metadataJson["dcat:keyword"]?.ToObject<List<string>>()
                };
                await _datasetStore.CreateOrUpdateDatasetRecordAsync(datasetInfo);
                ProgressLog.DatasetUpdated(datasetInfo);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update dataset record");
                throw new WorkerException(ex,
                    "Failed to update dataset record. Your repository is updated, but may not show in the portal.");
            }
        }

        private async Task AddCsvFilesToRepository(string repositoryDirectory, string datasetId, string csvFileName, string csvFileId, string csvmFileId)
        {
            try
            {
                ProgressLog.Info("Copying source CSV and metadata files to repository directory csv/{0}", datasetId);
                var datasetCsvDirPath = Path.Combine(repositoryDirectory, "csv", datasetId);
                if (!Directory.Exists(datasetCsvDirPath)) Directory.CreateDirectory(datasetCsvDirPath);
                var csvFilePath = Path.Combine(datasetCsvDirPath, csvFileName);
                var csvFileStream = await _jobFileStore.GetFileAsync(csvFileId);
                using (var csvOutStream = File.Open(csvFilePath, FileMode.Create, FileAccess.Write))
                {
                    csvFileStream.CopyTo(csvOutStream);
                }
                if (csvmFileId != null)
                {
                    var csvmFilePath = csvFilePath + "-metadata.json";
                    var csvmFileStream = await _jobFileStore.GetFileAsync(csvmFileId);
                    using (var csvmOutStream = File.Open(csvmFilePath, FileMode.Create, FileAccess.Write))
                    {
                        csvmFileStream.CopyTo(csvmOutStream);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy CSV/CSVM files");
                throw new WorkerException(ex, "Failed to copy CSV/CSVM files from upload to Github repository.");
            }
        }

        private static string MakeSafeTag(string tag)
        {
            return Regex.Replace(tag, @"[^a-zA-Z0-9]", "_", RegexOptions.None);
        }

        private async Task<ContactInfo> GetPublisherContactInfo(string ownerId, string repoId)
        {
            try
            {
                ProgressLog.Info("Attempting to retrieve publisher contact information from repository settings");
                // get repoSettings
                var repoSettings = await RepoSettingsStore.GetRepoSettingsAsync(ownerId, repoId);
                if (repoSettings?.DefaultPublisher != null)
                {
                    ProgressLog.Info("Returning publisher from repository settings");
                    return repoSettings.DefaultPublisher;
                }
                // no repo settings publisher, try at owner level
                ProgressLog.Info("No publisher info found in repository settings");
                if (ownerId != null)
                {
                    ProgressLog.Info("Attempting to retrieve publisher contact information from repository owner's settings");
                    var ownerSettings = await OwnerSettingsStore.GetOwnerSettingsAsync(ownerId);
                    if (ownerSettings?.DefaultPublisher != null)
                    {
                        ProgressLog.Info("Returning publisher from repository owner's settings");
                        return ownerSettings.DefaultPublisher;
                    }
                }
                // no settings / publisher found for that repo
                ProgressLog.Info("No publisher info found in repository owner's settings");
                return null;
            }
            catch (Exception)
            {
                ProgressLog.Error("Error when attempting to retrieve publisher contact information from repository/owner settings");
                return null;
            }

        }

        private async Task<Graph> GenerateDatasetGraphAsync(string csvPath, JObject metadataJson, Uri metadataUri)
        {
            var parser = new JsonMetadataParser(null, metadataUri);
            var tableGroup = new TableGroup();
            try
            {
                var tableMeta = parser.ParseTable(tableGroup, metadataJson);
                if (tableMeta == null)
                {
                    throw new WorkerException("CSV Conversion failed. Unable to read CSV table metadata.");
                }
            }
            catch (MetadataParseException ex)
            {
                Log.Error(ex, "Invalid CSV table metadata: " + ex.Message);
                throw new WorkerException(ex, "CSV conversion failed. Invalid CSV table metadata: " + ex.Message);
            }

            var graph = new Graph();
            ProgressLog.Info("Running CSV to RDF conversion");
            var graphHandler = new GraphHandler(graph);
            var tableResolver = new LocalTableResolver(tableGroup.Tables[0].Url, csvPath);
            var converter = new Converter(graphHandler, tableResolver, ConverterMode.Minimal, (msg) => ProgressLog.Error(msg), this, reportInterval: CsvConversionReportInterval);
            await converter.ConvertAsync(tableGroup);
            if (converter.Errors.Any())
            {
                foreach (var e in converter.Errors)
                {
                    ProgressLog.Error(e);
                }
                throw new WorkerException("One or more errors where encountered during the CSV to RDF conversion.");
            }
            return graph;
        }

        private Graph GenerateMetadataGraph(Uri datasetUri, Uri publisherIri, JObject metadataJson, IEnumerable<Uri> downloadUris, IGraph dataGraph)
        {
            var metadataGraph = new Graph();
            var metadataExtractor = new MetdataExtractor();
            ProgressLog.Info("Extracting dataset metadata");
            metadataExtractor.Run(metadataJson, metadataGraph, publisherIri, dataGraph.Triples.Count, DateTime.UtcNow);
            var dsNode = metadataGraph.CreateUriNode(datasetUri);
            var ddNode = metadataGraph.CreateUriNode(new Uri("http://rdfs.org/ns/void#dataDump"));
            var exampleResource = metadataGraph.CreateUriNode(new Uri("http://rdfs.org/ns/void#exampleResource"));
            foreach (var downloadUri in downloadUris)
            {
                metadataGraph.Assert(dsNode, ddNode, metadataGraph.CreateUriNode(downloadUri));
            }
            foreach (var distinctSubject in dataGraph.Triples.Select(t => t.Subject).OfType<IUriNode>().Distinct().Take(10))
            {
                metadataGraph.Assert(dsNode, exampleResource, distinctSubject);
            }
            return metadataGraph;
        }

        private JObject ExtractVoidMetadata(IGraph metadataGraph)
        {
            var metadata = new JObject();
            var dataDump = metadataGraph.CreateUriNode(new Uri("http://rdfs.org/ns/void#dataDump"));
            var tripleCount = metadataGraph.CreateUriNode(new Uri("http://rdfs.org/ns/void#triples"));
            var dataDumps = metadataGraph.GetTriplesWithPredicate(dataDump).Select(t => t.Object.ToString()).ToArray();
            var tripleCountValue = metadataGraph.GetTriplesWithPredicate(tripleCount).Select(t => t.Object)
                .OfType<ILiteralNode>().Select(lit => long.Parse(lit.Value)).FirstOrDefault();
            if (dataDumps.Length > 0)
            {
                if (dataDumps.Length == 1)
                {
                    metadata["void:dataDump"] = dataDumps[0];
                }
                else
                {
                    metadata["void:dataDump"] = new JArray(dataDumps);
                }
            }

            if (tripleCountValue > 0)
            {
                metadata["void:triples"] = tripleCountValue;
            }

            return metadata;
        }

        private Graph GenerateDefinitionsGraph(JObject metadataJson)
        {
            var definitionsGraph = new Graph();
            var metadataExtractor = new MetdataExtractor();
            ProgressLog.Info("Extracting column property definitions");
            metadataExtractor.GenerateColumnDefinitions(metadataJson, definitionsGraph);
            return definitionsGraph;
        }


        public void Report(int value)
        {
            ProgressLog.Info("CSV conversion processed {0} rows", value);
        }
    }

    internal class LocalTableResolver:ITableResolver
    {
        private readonly Dictionary<Uri, string> _lookup = new Dictionary<Uri, string>();
        public LocalTableResolver(Uri csvUri, string csvFilePath)
        {
            _lookup[csvUri] = csvFilePath;
        }

        public async Task<Stream> ResolveAsync(Uri tableUri)
        {
            return await Task.Run(() =>
            {
                if (_lookup.TryGetValue(tableUri, out var filePath))
                {
                    return File.OpenRead(filePath);
                }

                throw new FileNotFoundException("Could not resolve URI " + tableUri);
            });
        }

        public Task<JObject> ResolveJsonAsync(Uri jsonUri)
        {
            throw new NotImplementedException();
        }
    }
}
