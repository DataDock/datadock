using Nest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Common.Validators;

namespace DataDock.Common.Elasticsearch
{
    public class RepoSettingsStore : IRepoSettingsStore
    {
        private readonly IElasticClient _client;
        public RepoSettingsStore(IElasticClient client, ApplicationConfiguration config)
        {
            var indexName = config.RepoSettingsIndexName;
            Log.Debug("Create RepoSettingsStore. Index={indexName}", indexName);
            _client = client;
            // Ensure the index exists
            var indexExistsResponse = _client.IndexExists(indexName);
            if (!indexExistsResponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", indexName, typeof(JobInfo));
                var createIndexResponse = _client.CreateIndex(indexName,
                    c => c.Mappings(mappings => mappings.Map<RepoSettings>(m => m.AutoMap(-1))));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", indexName, createIndexResponse.DebugInformation);
                    throw new DataDockException(
                        $"Could not create index {indexName} for repo settings repository. Cause: {createIndexResponse.DebugInformation}");
                }
            }

            _client.ConnectionSettings.DefaultIndices[typeof(RepoSettings)] = indexName;
        }

        public async Task<IEnumerable<RepoSettings>> GetRepoSettingsForOwnerAsync(string ownerId)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));
            
            var search = new SearchDescriptor<RepoSettings>().Query(q => QueryHelper.FilterByOwnerId(ownerId));
            var response =
                await _client.SearchAsync<RepoSettings>(search);

            if (!response.IsValid)
            {
                throw new RepoSettingsStoreException(
                    $"Error retrieving repository settings for owner {ownerId}. Cause: {response.DebugInformation}");
            }

            if (response.Total < 1)
            {
                Log.Warning("No repository settings found for {ownerId}", ownerId);
                throw new RepoSettingsNotFoundException(ownerId);
            }
            return response.Documents;
        }

        public async Task<RepoSettings> GetRepoSettingsAsync(string ownerId, string repositoryId)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));
            if (repositoryId == null) throw new ArgumentNullException(nameof(repositoryId));
            var search = new SearchDescriptor<RepoSettings>().Query(q => QueryHelper.FilterByOwnerIdAndRepositoryId(ownerId, repositoryId));
            var response = await _client.SearchAsync<RepoSettings>(search);
            
            if (!response.IsValid)
            {
                throw new RepoSettingsStoreException(
                    $"Error retrieving repository settings for repo ID {repositoryId} on owner {ownerId}. Cause: {response.DebugInformation}");
            }

            if (response.Total < 1)
            {
                Log.Information("No settings found for {ownerId}/{repositoryId}", ownerId, repositoryId);
                throw new RepoSettingsNotFoundException(ownerId, repositoryId);
            }
            return response.Documents.FirstOrDefault();
        }

        public async Task<RepoSettings> GetRepoSettingsByIdAsync(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            try
            {
                var datasetInfo = await _client.GetAsync(new DocumentPath<RepoSettings>(id));
                return datasetInfo.Source;
            }
            catch (Exception e)
            {
                throw new RepoSettingsStoreException(
                    $"Error retrieving dataset with ID {id}. Cause: {e}");
            }
        }

        public async Task CreateOrUpdateRepoSettingsAsync(RepoSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrEmpty(settings.Id))
            {
                settings.Id = $"{settings.OwnerId}/{settings.RepositoryId}";
            }
            var validator = new RepoSettingsValidator();
            var validationResults = await validator.ValidateAsync(settings);
            if (!validationResults.IsValid)
            {
                throw new ValidationException("Invalid repo settings", validationResults);
            }
            var updateResponse = await _client.IndexDocumentAsync(settings);
            if (!updateResponse.IsValid)
            {
                throw new OwnerSettingsStoreException($"Error updating repo settings for owner/repo ID {settings.RepositoryId}");
            }

            await _client.RefreshAsync(Indices.Index<RepoSettings>());
        }

        public async Task<bool> DeleteRepoSettingsAsync(string ownerId, string repositoryId)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));
            if (repositoryId == null) throw new ArgumentNullException(nameof(repositoryId));

            var documentId = $"{ownerId}/{repositoryId}";
            var response = await _client.DeleteAsync<RepoSettings>(documentId);
            await _client.RefreshAsync(Indices.Index<RepoSettings>());
            return response.IsValid;
        }

        
    }
}

