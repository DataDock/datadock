using DataDock.Common;
using Nest;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;

namespace DataDock.Common.Elasticsearch
{
    public class DatasetStore : IDatasetStore
    {
        private readonly IElasticClient _client;

        public DatasetStore(IElasticClient client, ApplicationConfiguration config)
        {
            var indexName = config.DatasetIndexName;
            Log.Debug("Create DatasetStore. Index={indexName}", indexName);
            _client = client;
            // Ensure the index exists
            var indexExistsReponse = _client.IndexExists(indexName);
            if (!indexExistsReponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", indexName, typeof(DatasetInfo));
                var createIndexResponse = _client.CreateIndex(indexName, c => c.Mappings(
                    mappings => mappings.Map<DatasetInfo>(m => m.AutoMap(-1))));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", indexName, createIndexResponse.DebugInformation);
                    throw new DataDockException(
                        $"Could not create index {indexName} for DatasetStore. Cause: {createIndexResponse.DebugInformation}");
                }
            }
            _client.ConnectionSettings.DefaultIndices[typeof(DatasetInfo)] = indexName;
        }

        public async Task<IEnumerable<DatasetInfo>> GetRecentlyUpdatedDatasetsAsync(int limit, bool showHidden = false)
        {
            var search = new SearchDescriptor<DatasetInfo>();
            if (!showHidden)
            {
                // add query to filter by only those datasets with showOnHomepage = true
                search.Query(q => QueryHelper.FilterByShowOnHomepage());
            }
            var rawQuery = "";
#if DEBUG
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
#endif
            var searchResponse =
                await _client.SearchAsync<DatasetInfo>(search
                    .Skip(0)
                    .Take(limit)
                    .Sort(s => s.Field(f => f.Field("lastModified").Order(SortOrder.Descending))));

            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException("No datasets found");
            return searchResponse.Documents;
        }

        public async Task<IEnumerable<DatasetInfo>> GetRecentlyUpdatedDatasetsForOwnersAsync(string[] ownerIds, int skip, int take, bool showHidden = false)
        {
           
            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterByOwnerIds(ownerIds, showHidden));
            var rawQuery = "";
#if DEBUG
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
#endif
            var searchResponse =
                await _client.SearchAsync<DatasetInfo>(search
                    .Skip(skip)
                    .Take(take)
                    .Sort(s => s.Field(f => f.Field("lastModified").Order(SortOrder.Descending))));

            var ownerIdsString = string.Join(", ", ownerIds);
            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for owner IDs {ownerIdsString}. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException($"No dataset found for owners '{ownerIdsString}'");
            return searchResponse.Documents;
        }

        public async Task<IEnumerable<DatasetInfo>> GetRecentlyUpdatedDatasetsForRepositoriesAsync(string ownerId, string[] repositoryIds, int skip, int take, bool showHidden = false)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterByOwnerIdAndRepositoryIds(ownerId, repositoryIds, showHidden));
            var rawQuery = "";
#if DEBUG
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
#endif
            var searchResponse =
                await _client.SearchAsync<DatasetInfo>(search
                    .Skip(skip)
                    .Take(take)
                    .Sort(s => s.Field(f => f.Field("lastModified").Order(SortOrder.Descending))));

            var repositoryIdsString = string.Join(", ", repositoryIds);
            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for repo IDs {repositoryIdsString} on owner {ownerId}. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException($"No dataset found for owner '{ownerId}', repositories: '{repositoryIdsString}'");
            return searchResponse.Documents;

        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForOwnerAsync(string ownerId, int skip, int take)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));

            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterByOwnerId(ownerId));
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
                await _client.SearchAsync<DatasetInfo>(search.Skip(skip).Take(take));
            
            if (!response.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for owner {ownerId}. Cause: {response.DebugInformation}");
            }
            if (response.Total < 1) throw new DatasetNotFoundException($"No dataset found for owner '{ownerId}'");
            return response.Documents;
        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForRepositoryAsync(string ownerId, string repositoryId, int skip, int take)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            if (string.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));

            var rawQuery = "";
            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterByOwnerIdAndRepositoryId(ownerId, repositoryId));
#if DEBUG
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
#endif
            var response = await _client.SearchAsync<DatasetInfo>(search.Skip(skip).Take(take));
            
            if (!response.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for repo ID {repositoryId} on owner {ownerId}. Query: {rawQuery} Cause: {response.DebugInformation}");
            }

            if (response.Total < 1)
            {
                Log.Information($"No datasets found with query {rawQuery}");
                throw new DatasetNotFoundException($"No datasets found for owner '{ownerId}', repository: '{repositoryId}'");
            }
            return response.Documents;
        }
        
        public async Task<DatasetInfo> GetDatasetInfoAsync(string ownerId, string repositoryId, string datasetId)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            if (string.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));
            if (string.IsNullOrEmpty(datasetId)) throw new ArgumentNullException(nameof(datasetId));

            var rawQuery = "";
            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterByOwnerIdAndRepositoryIdAndDatasetId(ownerId, repositoryId, datasetId));
#if DEBUG
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
#endif
            var response =
                await _client.SearchAsync<DatasetInfo>(search);

            if (!response.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving dataset for dataset ID {datasetId}, repo ID {repositoryId} on owner {ownerId}. Cause: {response.DebugInformation}");
            }

            if (response.Total < 1)
            {
                Log.Warning($"No settings found with query {rawQuery}");
                throw new DatasetNotFoundException($"No dataset found for owner '{ownerId}', repository: '{repositoryId}', dataset: '{datasetId}' ");
            }
            return response.Documents.FirstOrDefault();
        }

        public async Task<DatasetInfo> GetDatasetInfoByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            try
            {
                var datasetInfo = await _client.GetAsync<DatasetInfo>(new DocumentPath<DatasetInfo>(id));
                return datasetInfo.Source;
            }
            catch (Exception e)
            {
                throw new DatasetStoreException(
                    $"Error retrieving dataset by ID {id}. Cause: {e.ToString()}");
            }
        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForTagsAsync(string[] tags, int skip = 0, int take = 25, bool matchAll = false, bool showHidden = false)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterByTags(tags, matchAll, showHidden));
            var rawQuery = "";
#if DEBUG
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
#endif
            var searchResponse =
                await _client.SearchAsync<DatasetInfo>(search
                .From(skip).Size(take));

            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for tags {string.Join(", ", tags)}. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException($"No datasets found for tags '{string.Join(", ", tags)}'");
            return searchResponse.Documents;
        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForTagsAsync(string ownerId, string[] tags, int skip = 0, int take = 25, bool matchAll = false, bool showHidden = false)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));

            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterOwnerByTags(ownerId, tags, matchAll, showHidden));
            var rawQuery = "";
#if DEBUG
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
#endif
            var searchResponse =
                await _client.SearchAsync<DatasetInfo>(search
                    .From(skip).Size(take));

            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for owner ID {ownerId} with tags {string.Join(", ", tags)}. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException($"No datasets for owner ID {ownerId} with tags {string.Join(", ", tags)}.");
            return searchResponse.Documents;
        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForTagsAsync(string ownerId, string repositoryId, string[] tags, int skip = 0, int take = 25, bool matchAll = false,
            bool showHidden = false)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));

            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterRepositoryByTags(ownerId, repositoryId, tags, matchAll, showHidden));
            var rawQuery = "";
#if DEBUG
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
#endif
            var searchResponse =
                await _client.SearchAsync<DatasetInfo>(search
                .From(skip).Size(take));

            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for repository {ownerId}/{repositoryId} with tags {string.Join(", ", tags)}. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException($"No datasets found for repository {ownerId}/{repositoryId} with tags {string.Join(", ", tags)}.");
            return searchResponse.Documents;
        }

        public async Task<DatasetInfo> CreateOrUpdateDatasetRecordAsync(DatasetInfo datasetInfo)
        {
            if (datasetInfo == null) throw new ArgumentNullException();
            if (string.IsNullOrEmpty(datasetInfo.Id))
            {
                datasetInfo.Id = $"{datasetInfo.OwnerId}/{datasetInfo.RepositoryId}/{datasetInfo.DatasetId}";
            }
            var indexResponse =await _client.IndexDocumentAsync(datasetInfo);
            if (!indexResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Failed to index dataset record. Cause: {indexResponse.DebugInformation}");
            }
            return datasetInfo;
        }

        public async Task<bool> DeleteDatasetsForOwnerAsync(string ownerId)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));
            var deleteResponse = await _client.DeleteByQueryAsync<DatasetInfo>(s => s.Query(q => QueryHelper.FilterByOwnerId(ownerId)));
            if (!deleteResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Failed to delete all datasets owned by {ownerId}");
            }
            return true;
        }

        public async Task<bool> DeleteDatasetAsync(string ownerId, string repositoryId, string datasetId)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));
            if (repositoryId == null) throw new ArgumentNullException(nameof(repositoryId));
            if (datasetId == null) throw new ArgumentNullException(nameof(datasetId));

            var documentId = $"{ownerId}/{repositoryId}/{datasetId}";
            var response = await _client.DeleteAsync<DatasetInfo>(documentId);
            return response.IsValid;
        }
        
    }
}
