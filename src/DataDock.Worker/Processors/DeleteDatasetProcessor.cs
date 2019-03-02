using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Common;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using Serilog;

namespace DataDock.Worker.Processors
{
    public class DeleteDatasetProcessor : PublishingJobProcessor
    {
        private readonly GitCommandProcessor _git;
        private readonly IDatasetStore _datasetStore;
        private readonly IDataDockRepositoryFactory _repositoryFactory;

        public DeleteDatasetProcessor(
            WorkerConfiguration configuration,
            GitCommandProcessor gitProcessor,
            IGitHubClientFactory gitHubClientFactory,
            IOwnerSettingsStore ownerSettingsStore,
            IRepoSettingsStore repoSettingsStore,
            IDatasetStore datasetStore,
            IDataDockRepositoryFactory repositoryFactory) : base(configuration, ownerSettingsStore, repoSettingsStore,
            gitHubClientFactory)
        {
            _git = gitProcessor;
            _datasetStore = datasetStore;
            _repositoryFactory = repositoryFactory;
        }

        protected override async Task RunJob(JobInfo jobInfo, UserAccount userInfo)
        {
            var targetDirectory = Path.Combine(Configuration.RepoBaseDir, jobInfo.JobId);
            Log.Information("Using local directory {localDirPath}", targetDirectory);
            Log.Information("Clone Repository: {gitRepositoryUrl} => {targetDirectory}", jobInfo.GitRepositoryUrl, targetDirectory);
            await _git.CloneRepository(jobInfo.GitRepositoryUrl, targetDirectory, AuthenticationToken, userInfo);

            var datasetIri = new Uri(jobInfo.DatasetIri);

            DeleteCsvAndMetadata(targetDirectory, jobInfo.DatasetId, ProgressLog);
            var dataDockRepository = _repositoryFactory.GetRepositoryForJob(jobInfo, ProgressLog);
            dataDockRepository.DeleteDataset(datasetIri);
            await UpdateHtmlPagesAsync(dataDockRepository, null);

            if (await _git.CommitChanges(targetDirectory, $"Deleted dataset {datasetIri}", userInfo))
            {
                await _git.PushChanges(jobInfo.GitRepositoryUrl, targetDirectory, AuthenticationToken);
            }

            try
            {
                await _datasetStore.DeleteDatasetAsync(jobInfo.OwnerId, jobInfo.RepositoryId, jobInfo.DatasetId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to remove dataset record.");
                throw new WorkerException(ex, "Failed to remove dataset record. Your repository is updated but the dataset may still show in the main lodlab portal");
            }

            Log.Information("Dataset Deleted: {OwnerId}/RepositoryId/{DatasetId}",
                jobInfo.OwnerId, jobInfo.RepositoryId, jobInfo.DatasetId);
            ProgressLog.DatasetDeleted(jobInfo.OwnerId, jobInfo.RepositoryId, jobInfo.DatasetId);

        }


        private static void DeleteCsvAndMetadata(string baseDirectory, string datasetId, IProgressLog progressLog)
        {
            Log.Information("DeleteCsvAndMetadata: {baseDirectory}, {datasetId}", baseDirectory, datasetId);
            try
            {
                progressLog.Info("Deleting source CSV and CSV metadata files");
                var csvPath = Path.Combine(baseDirectory, "csv", datasetId);
                Directory.Delete(csvPath, true);
            }
            catch (Exception ex)
            {
                progressLog.Exception(ex, "Error deleting source CSV and CSV metadata files");
                throw;
            }
        }

    }
}
