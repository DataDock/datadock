using System.Collections.Generic;
using System.Threading.Tasks;
using DataDock.Common.Models;

namespace DataDock.Common.Stores
{
    public interface IDatasetStore
    {
        /// <summary>
        /// Get the N most recently updated datasets across all owners and repositories
        /// </summary>
        /// <param name="limit">The number of results to return</param>
        /// <param name="showHidden">Whether to include datasets that are hidden from the front page. Defaults to false</param>
        /// <returns></returns>
        Task<IEnumerable<DatasetInfo>> GetRecentlyUpdatedDatasetsAsync(int limit, bool showHidden = false);

        /// <summary>
        /// Get a list of the most recently updated datasets that for specific owners
        /// </summary>
        /// <param name="ownerIds">A list of owner IDs to match</param>
        /// <param name="skip">The number of results to skip</param>
        /// <param name="take">The number of results to return</param>
        /// <param name="showHidden">Whether to include datasets that are hidden from the front page. Defaults to false</param>
        /// <returns>A list of <see cref="DatasetInfo"/> instances ordered by last modified date (most recent first)</returns>
        Task<IEnumerable<DatasetInfo>> GetRecentlyUpdatedDatasetsForOwnersAsync(string[] ownerIds, int skip, int take, bool showHidden = false);

        /// <summary>
        /// Get a list of the most recently updated datasets that are in one of the specified repositories
        /// </summary>
        /// <param name="ownerId">The ID of the owner that the repositories belong to</param>
        /// <param name="repositoryIds">A list of repository IDs to match</param>
        /// <param name="skip">The number of results to skip</param>
        /// <param name="take">The number of results to return</param>
        /// <param name="showHidden">Whether to include datasets that are hidden from the front page. Defaults to false</param>
        /// <returns>A list of <see cref="DatasetInfo"/> instances ordered by last modified date (most recent first)</returns>
        Task<IEnumerable<DatasetInfo>> GetRecentlyUpdatedDatasetsForRepositoriesAsync(string ownerId, string[] repositoryIds, int skip, int take, bool showHidden = false);

        /// <summary>
        /// Get a list datasets for a specific owner
        /// </summary>
        /// <param name="ownerId">The owner (user / organization)</param>
        /// <param name="skip">The number of results to skip</param>
        /// <param name="take">The number of results to return</param>
        /// <returns>A list of <see cref="DatasetInfo"/> instances</returns>

        Task<IEnumerable<DatasetInfo>> GetDatasetsForOwnerAsync(string ownerId, int skip, int take);

        /// <summary>
        /// Get a list datasets for a specific repository
        /// </summary>
        /// <param name="ownerId">The owner of the repository</param>
        /// <param name="repositoryId">The repository ID to match</param>
        /// <param name="skip">The number of results to skip</param>
        /// <param name="take">The number of results to return</param>
        /// <returns>A list of <see cref="DatasetInfo"/> instances</returns>
        Task<IEnumerable<DatasetInfo>> GetDatasetsForRepositoryAsync(string ownerId, string repositoryId, int skip, int take);

        Task<DatasetInfo> GetDatasetInfoAsync(string ownerId, string repositoryId, string datasetId);

        Task<DatasetInfo> GetDatasetInfoByIdAsync(string id);

        Task<IEnumerable<DatasetInfo>> GetDatasetsForTagsAsync(string[] tags, int skip, int take, bool matchAll = false, bool showHidden = false);

        /// <summary>
        /// Get all datasets in repositories owned by the specified owner with a specified tag
        /// </summary>
        /// <remarks>Currently implemented as an exact match on tag string</remarks>
        /// <param name="ownerId"></param>
        /// <param name="tags">An array of one or more tags to match</param>
        /// <param name="take"></param>
        /// <param name="matchAll">True to require a matching dataset to match all of the specified tags, false to require a matching dataset to match any one of the specified tags. Defaults to false</param>
        /// <param name="showHidden">True to include datasets that are hidden from the DD homepage in the results. Default to false</param>
        /// <param name="skip"></param>
        /// <returns></returns>
        Task<IEnumerable<DatasetInfo>> GetDatasetsForTagsAsync(string ownerId, string[] tags, int skip, int take, bool matchAll = false, bool showHidden = false);

        /// <summary>
        /// Get all datasets in a specific repository with a specified tag
        /// </summary>
        /// <remarks>Currently implemented as an exact match on tag string</remarks>
        /// <param name="ownerId">The owner ID</param>
        /// <param name="repositoryId">The ID of the repository to search within</param>
        /// <param name="tags">An array of one or more tags to match</param>
        /// <param name="matchAll">True to require a matching dataset to match all of the specified tags, false to require a matching dataset to match any one of the specified tags. Defaults to false</param>
        /// <param name="showHidden">True to include datasets that are hidden from the DD homepage in the results. Default to false</param>
        /// <returns></returns>
        Task<IEnumerable<DatasetInfo>> GetDatasetsForTagsAsync(string ownerId, string repositoryId, string[] tags, int skip, int take, bool matchAll = false, bool showHidden = false);

        Task<DatasetInfo> CreateOrUpdateDatasetRecordAsync(DatasetInfo datasetInfo);

        /// <summary>
        /// Delete all dataset records for a given owner
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns>true if successful</returns>
        Task<bool> DeleteDatasetsForOwnerAsync(string ownerId);

        Task<bool> DeleteDatasetAsync(string ownerId, string repositoryId, string datasetId);
    }
}
