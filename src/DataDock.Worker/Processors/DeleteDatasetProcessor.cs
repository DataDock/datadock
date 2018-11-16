using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using Serilog;

namespace DataDock.Worker.Processors
{
    public class DeleteDatasetProcessor : IDataDockProcessor
    {
        private readonly WorkerConfiguration _configuration;
        private readonly GitCommandProcessor _git;
        private readonly IDatasetStore _datasetStore;
        private readonly IDataDockRepository _dataDockRepository;

        public DeleteDatasetProcessor(
            WorkerConfiguration configuration,
            GitCommandProcessor gitProcessor,
            IDatasetStore datasetStore,
            IDataDockRepository dataDockRepository)
        {
            _configuration = configuration;
            _git = gitProcessor;
            _datasetStore = datasetStore;
            _dataDockRepository = dataDockRepository;
        }

        public async Task ProcessJob(JobInfo jobInfo, UserAccount userAccount, IProgressLog progressLog)
        {
            var authenticationClaim =
                userAccount.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.GitHubAccessToken));
            var authenticationToken = authenticationClaim?.Value;
            if (string.IsNullOrEmpty(authenticationToken))
            {
                Log.Error("No authentication token found for user {userId}", userAccount.UserId);
                progressLog.Error("Could not find a valid GitHub access token for this user account. Please check your account settings.");
            }

            var targetDirectory = Path.Combine(_configuration.RepoBaseDir, jobInfo.JobId);
            Log.Information("Using local directory {localDirPath}", targetDirectory);
            Log.Information("Clone Repository: {gitRepositoryUrl} => {targetDirectory}", jobInfo.GitRepositoryUrl, targetDirectory);
            await _git.CloneRepository(jobInfo.GitRepositoryUrl, targetDirectory, authenticationToken, userAccount);

            var datasetIri = new Uri(jobInfo.DatasetIri);

            DeleteCsvAndMetadata(targetDirectory, jobInfo.DatasetId, progressLog);
            _dataDockRepository.DeleteDataset(datasetIri);
            _dataDockRepository.Publish();

            if (await _git.CommitChanges(targetDirectory, $"Deleted dataset {datasetIri}", userAccount))
            {
                await _git.PushChanges(jobInfo.GitRepositoryUrl, targetDirectory, authenticationToken);
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
