using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Common;
using DataDock.CsvWeb.Metadata;
using DataDock.CsvWeb.Parsing;
using DataDock.CsvWeb.Rdf;
using DataDock.Worker.Liquid;
using Newtonsoft.Json.Linq;
using Serilog;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;

namespace DataDock.Worker.Processors
{
    public class ImportJobProcessor : IDataDockProcessor, IProgress<int>
    {
        private readonly WorkerConfiguration _configuration;
        private readonly GitCommandProcessor _git;
        private readonly IGitHubClientFactory _gitHubClientFactory;
        private readonly IDatasetStore _datasetStore;
        private readonly IOwnerSettingsStore _ownerSettingsStore;
        private readonly IRepoSettingsStore _repoSettingsStore;
        private readonly IFileStore _jobFileStore;
        private IProgressLog _progressLog;
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
            IDataDockUriService dataDockUriService)
        {
            _configuration = configuration;
            _git = gitProcessor;
            _gitHubClientFactory = gitHubClientFactory;
            _datasetStore = datasetStore;
            _ownerSettingsStore = ownerSettingsStore;
            _repoSettingsStore = repoSettingsStore;
            _jobFileStore = jobFileStore;
            _dataDataDockRepositoryFactory = dataDockRepositoryFactory;
            _dataDockUriService = dataDockUriService;
        }

        public async Task ProcessJob(JobInfo job, UserAccount userAccount, IProgressLog progressLog)
        {
            _progressLog = progressLog;
            _progressLog.Info("Starting import job processing for " + userAccount.UserId);

            var authenticationClaim =
                userAccount.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.GitHubAccessToken));
            var authenticationToken = authenticationClaim?.Value;
            if (string.IsNullOrEmpty(authenticationToken))
            {
                Log.Error("No authentication token found for user {userId}", userAccount.UserId);
                _progressLog.Error("Could not find a valid GitHub access token for this user account. Please check your account settings.");
            }

            var targetDirectory = Path.Combine(_configuration.RepoBaseDir, job.JobId);
            Log.Information("Using local directory {localDirPath}", targetDirectory);

            // Clone the repository
            await _git.CloneRepository(job.GitRepositoryUrl, targetDirectory, authenticationToken, userAccount);

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
            using (var csvReader = File.OpenText(csvPath))
            {
                datasetGraph = GenerateDatasetGraph(csvReader, metadataJson);
            }
            IGraph metadataGraph = GenerateMetadataGraph(datasetUri, publisherIri, metadataJson,
                new[] { ntriplesDownloadLink, csvDownloadLink }, datasetGraph);

            IGraph definitionsGraph = GenerateDefinitionsGraph(metadataJson);

            

            var dataDataDockRepository = _dataDataDockRepositoryFactory.GetRepositoryForJob(job, progressLog);
            dataDataDockRepository.UpdateDataset(
                datasetGraph, datasetUri, job.OverwriteExistingData,
                metadataGraph, datasetMetadataGraphIri, 
                definitionsGraph, definitionsGraphIri, 
                publisherIri, publisher,
                "", "",
                rootMetadataGraphIri);

            var portalInfo = await GetPortalSettingsInfo(job.OwnerId, job.RepositoryId, authenticationToken);
            //TODO get datadock-publish-url from config? page template are always remote as they are pushed to github
            var templateVariables =
                new Dictionary<string, object>
                {
                    {"datadock-publish-url", "https://datadock.io" }, 
                    {"owner-id", job.OwnerId},
                    {"repo-id", job.RepositoryId},
                    {"portal-info", portalInfo},
                };

            dataDataDockRepository.Publish(
                new[] { datasetUri, datasetMetadataGraphIri, rootMetadataGraphIri },
                templateVariables);

            // Add and Commit all changes
            if (await _git.CommitChanges(targetDirectory,
                $"Added {job.CsvFileName} to dataset {job.DatasetIri}", userAccount))
            {
                await _git.PushChanges(job.GitRepositoryUrl, targetDirectory, authenticationToken);
                await _git.MakeRelease(datasetGraph, releaseTag, job.OwnerId, job.RepositoryId, job.DatasetId, targetDirectory, authenticationToken);
            }

            // Update the dataset repository
            try
            {
                await _datasetStore.CreateOrUpdateDatasetRecordAsync(new DatasetInfo
                {
                    OwnerId = job.OwnerId,
                    RepositoryId = job.RepositoryId,
                    DatasetId = job.DatasetId,
                    LastModified = DateTime.UtcNow,
                    CsvwMetadata = metadataJson,
                    ShowOnHomePage = job.IsPublic,
                    Tags = metadataJson["dcat:keyword"]?.ToObject<List<string>>()
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update dataset record");
                throw new WorkerException(ex,
                    "Failed to update dataset record. Your repository is updated, but may not show in the main lodlab portal.");
            }

        }

        private async Task AddCsvFilesToRepository(string repositoryDirectory, string datasetId, string csvFileName, string csvFileId, string csvmFileId)
        {
            try
            {
                _progressLog.Info("Copying source CSV and metadata files to repository directory csv/{0}", datasetId);
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
                _progressLog.Info("Attempting to retrieve publisher contact information from repository settings");
                // get repoSettings
                var repoSettings = await _repoSettingsStore.GetRepoSettingsAsync(ownerId, repoId);
                if (repoSettings?.DefaultPublisher != null)
                {
                    _progressLog.Info("Returning publisher from repository settings");
                    return repoSettings.DefaultPublisher;
                }
                // no repo settings publisher, try at owner level
                _progressLog.Info("No publisher info found in repository settings");
                if (ownerId != null)
                {
                    _progressLog.Info("Attempting to retrieve publisher contact information from repository owner's settings");
                    var ownerSettings = await _ownerSettingsStore.GetOwnerSettingsAsync(ownerId);
                    if (ownerSettings?.DefaultPublisher != null)
                    {
                        _progressLog.Info("Returning publisher from repository owner's settings");
                        return ownerSettings.DefaultPublisher;
                    }
                }
                // no settings / publisher found for that repo
                _progressLog.Info("No publisher info found in repository owner's settings");
                return null;
            }
            catch (Exception)
            {
                _progressLog.Error("Error when attempting to retrieve publisher contact information from repository/owner settings");
                return null;
            }

        }

        private async Task<PortalInfoDrop> GetPortalSettingsInfo(string ownerId, string repoId, string authenticationToken)
        {
            try
            {
                _progressLog.Info("Attempting to retrieve portal settings information from owner settings");
                if (ownerId != null)
                {
                    var portalInfo = new PortalInfoDrop
                    {
                        OwnerId = ownerId,
                        RepositoryName = repoId
                    };

                    _progressLog.Info("Attempting to retrieve publisher contact information from repository owner's settings");
                    var ownerSettings = await _ownerSettingsStore.GetOwnerSettingsAsync(ownerId);
                    if (ownerSettings != null)
                    {
                        portalInfo.IsOrg = ownerSettings.IsOrg;
                        portalInfo.ShowDashboardLink = ownerSettings.DisplayDataDockLink;
                        if (!string.IsNullOrEmpty(ownerSettings.TwitterHandle)) portalInfo.Twitter = ownerSettings.TwitterHandle;

                        var client = _gitHubClientFactory.CreateClient(authenticationToken);
                        if (ownerSettings.IsOrg)
                        {
                            var org = await client.Organization.Get(ownerId);
                            if (org == null) return portalInfo;

                            portalInfo.OwnerDisplayName = org.Name;
                            if (ownerSettings.DisplayGitHubBlogUrl) portalInfo.Website = org.Blog;
                            if (ownerSettings.DisplayGitHubAvatar) portalInfo.LogoUrl = org.AvatarUrl;
                            if (ownerSettings.DisplayGitHubDescription) portalInfo.Description = org.Bio;
                            if (ownerSettings.DisplayGitHubBlogUrl) portalInfo.Website = org.Blog;
                            if (ownerSettings.DisplayGitHubLocation) portalInfo.Location = org.Location;
                            if (ownerSettings.DisplayGitHubIssuesLink) portalInfo.GitHubHtmlUrl = org.HtmlUrl;
                        }
                        else
                        {
                            var user = await client.User.Get(ownerId);
                            if (user == null) return portalInfo;

                            portalInfo.OwnerDisplayName = user.Name;
                            if (ownerSettings.DisplayGitHubBlogUrl) portalInfo.Website = user.Blog;
                            if (ownerSettings.DisplayGitHubAvatar) portalInfo.LogoUrl = user.AvatarUrl;
                            if (ownerSettings.DisplayGitHubDescription) portalInfo.Description = user.Bio;
                            if (ownerSettings.DisplayGitHubBlogUrl) portalInfo.Website = user.Blog;
                            if (ownerSettings.DisplayGitHubLocation) portalInfo.Location = user.Location;
                            if (ownerSettings.DisplayGitHubIssuesLink) portalInfo.GitHubHtmlUrl = user.HtmlUrl;
                        }
                    }
                    _progressLog.Info("Looking up repository portal search buttons from settings for {0} repository.", repoId);

                    var repoSettings = await _repoSettingsStore.GetRepoSettingsAsync(ownerId, repoId);
                    var repoSearchButtons = repoSettings?.SearchButtons;
                    if (!string.IsNullOrEmpty(repoSearchButtons))
                    {

                        portalInfo.RepoSearchButtons = GetSearchButtons(repoSearchButtons);

                    }
                    return portalInfo;
                }
                // no settings 
                _progressLog.Info("No owner settings found");
                return null;
            }
            catch (Exception e)
            {
                _progressLog.Error("Error when attempting to retrieve portal information from owner settings");
                return null;
            }

        }

        private List<SearchButtonDrop> GetSearchButtons(string searchButtonsString)
        {
            var sbSplit = searchButtonsString.Split(',');
            var searchButtons = new List<SearchButtonDrop>();
            foreach (var b in sbSplit)
            {
                var sb = new SearchButtonDrop();
                if (b.IndexOf(';') >= 0)
                {
                    // has different button text
                    var bSplit = b.Split(';');
                    sb.Tag = bSplit[0];
                    sb.Text = bSplit[1];
                    searchButtons.Add(sb);
                }
                else
                {
                    sb.Tag = b;
                    searchButtons.Add(sb);
                }
            }
            return searchButtons;
        }

        private Graph GenerateDatasetGraph(TextReader csvReader, JObject metadataJson)
        {
            var parser = new JsonMetadataParser(null);
            Table tableMeta;
            try
            {
                tableMeta = parser.Parse(metadataJson) as Table;
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
            _progressLog.Info("Running CSV to RDF conversion");
            var graphHandler = new GraphHandler(graph);
            var converter = new Converter(tableMeta, graphHandler, (msg) => _progressLog.Error(msg), this, reportInterval: CsvConversionReportInterval);
            converter.Convert(csvReader);
            if (converter.Errors.Any())
            {
                foreach (var e in converter.Errors)
                {
                    _progressLog.Error(e);
                }
                throw new WorkerException("One or more errors where encountered during the CSV to RDF conversion.");
            }
            return graph;
        }

        private Graph GenerateMetadataGraph(Uri datasetUri, Uri publisherIri, JObject metadataJson, IEnumerable<Uri> downloadUris, IGraph dataGraph)
        {
            var metadataGraph = new Graph();
            var metadataExtractor = new MetdataExtractor();
            _progressLog.Info("Extracting dataset metadata");
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

        private Graph GenerateDefinitionsGraph(JObject metadataJson)
        {
            var definitionsGraph = new Graph();
            var metadataExtractor = new MetdataExtractor();
            _progressLog.Info("Extracting column property definitions");
            metadataExtractor.GenerateColumnDefinitions(metadataJson, definitionsGraph);
            return definitionsGraph;
        }


        public void Report(int value)
        {
            _progressLog.Info("CSV conversion processed {0} rows", value);
        }
    }


}
