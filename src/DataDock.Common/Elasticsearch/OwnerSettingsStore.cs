using Nest;
using Serilog;
using System;
using System.Threading.Tasks;
using DataDock.Common;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Common.Validators;

namespace DataDock.Common.Elasticsearch
{
    public class OwnerSettingsStore : IOwnerSettingsStore
    {
        private readonly IElasticClient _client;
        public OwnerSettingsStore(IElasticClient client, ApplicationConfiguration config)
        {
            var indexName = config.OwnerSettingsIndexName;
            Log.Debug("Create OwnerSettingsStore. Index={indexName}", indexName);
            _client = client;
            // Ensure the index exists
            var indexExistsReponse = _client.IndexExists(indexName);
            if (!indexExistsReponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", indexName, typeof(JobInfo));
                var createIndexResponse = _client.CreateIndex(indexName,
                    c => c.Mappings(mappings => mappings.Map<OwnerSettings>(m => m.AutoMap(-1))));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", indexName, createIndexResponse.DebugInformation);
                    throw new DataDockException(
                        $"Could not create index {indexName} for owner settings repository. Cause: {createIndexResponse.DebugInformation}");
                }
            }

            _client.ConnectionSettings.DefaultIndices[typeof(OwnerSettings)] = indexName;
        }

        public async Task<OwnerSettings> GetOwnerSettingsAsync(string ownerId)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));
            var response = await _client.GetAsync<OwnerSettings>(ownerId);
            if (!response.IsValid)
            {
                if (!response.Found) throw new OwnerSettingsNotFoundException(ownerId);
                throw new OwnerSettingsStoreException(
                    $"Error retrieving owner settings for owner ID {ownerId}. Cause: {response.DebugInformation}");
            }
            return response.Source;
        }

        public async Task CreateOrUpdateOwnerSettingsAsync(OwnerSettings ownerSettings)
        {
            if (ownerSettings == null) throw new ArgumentNullException(nameof(ownerSettings));
            var validator = new OwnerSettingsValidator();
            var validationResults = await validator.ValidateAsync(ownerSettings);
            if (!validationResults.IsValid)
            {
                throw new ValidationException("Invalid owner settings", validationResults);
            }
            var updateResponse = await _client.IndexDocumentAsync(ownerSettings);
            if (!updateResponse.IsValid)
            {
                throw new OwnerSettingsStoreException($"Error udpating owner settings for owner ID {ownerSettings.OwnerId}");
            }
        }

        public async Task<bool> DeleteOwnerSettingsAsync(string ownerId)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));
            var response = await _client.DeleteAsync<OwnerSettings>(ownerId);
            return response.IsValid;
        }
    }
}

