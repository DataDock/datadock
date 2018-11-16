using Nest;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataDock.Common;
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
            var indexExistsReponse = _client.IndexExists(indexName);
            if (!indexExistsReponse.Exists)
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
            var rawQuery = "";
#if DEBUG
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
#endif
            var response =
                await _client.SearchAsync<RepoSettings>(search);

            if (!response.IsValid)
            {
                throw new RepoSettingsStoreException(
                    $"Error retrieving repository settings for owner {ownerId}. Cause: {response.DebugInformation}");
            }

            if (response.Total < 1)
            {
                Log.Warning($"No settings found with query {rawQuery}");
                throw new RepoSettingsNotFoundException(ownerId);
            }
            return response.Documents;
        }

        public async Task<RepoSettings> GetRepoSettingsAsync(string ownerId, string repositoryId)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));
            if (repositoryId == null) throw new ArgumentNullException(nameof(repositoryId));
            var rawQuery = "";
            var search = new SearchDescriptor<RepoSettings>().Query(q => QueryHelper.FilterByOwnerIdAndRepositoryId(ownerId, repositoryId));
#if DEBUG
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
#endif

            var response =
                await _client.SearchAsync<RepoSettings>(search);
            
            if (!response.IsValid)
            {
                throw new RepoSettingsStoreException(
                    $"Error retrieving repository settings for repo ID {repositoryId} on owner {ownerId}. Cause: {response.DebugInformation}");
            }

            if (response.Total < 1)
            {
                Log.Warning($"No settings found with query {rawQuery}");
                throw new RepoSettingsNotFoundException(ownerId, repositoryId);
            }
            return response.Documents.FirstOrDefault();
        }

        public async Task<RepoSettings> GetRepoSettingsByIdAsync(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            try
            {
                var datasetInfo = await _client.GetAsync<RepoSettings>(new DocumentPath<RepoSettings>(id));
                return datasetInfo.Source;
            }
            catch (Exception e)
            {
                throw new RepoSettingsStoreException(
                    $"Error retrieving dataset with ID {id}. Cause: {e.ToString()}");
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
        }

        public async Task<bool> DeleteRepoSettingsAsync(string ownerId, string repositoryId)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));
            if (repositoryId == null) throw new ArgumentNullException(nameof(repositoryId));

            string documentId = $"{ownerId}/{repositoryId}";
            var response = await _client.DeleteAsync<RepoSettings>(documentId);
            return response.IsValid;
        }

        
    }
}

