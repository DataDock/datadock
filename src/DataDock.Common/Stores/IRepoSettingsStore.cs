using System.Collections.Generic;
using System.Threading.Tasks;
using DataDock.Common.Models;

namespace DataDock.Common.Stores
{
    public interface IRepoSettingsStore
    {
        Task<IEnumerable<RepoSettings>> GetRepoSettingsForOwnerAsync(string ownerId);
        Task<RepoSettings> GetRepoSettingsAsync(string ownerId, string repositoryId);
        Task CreateOrUpdateRepoSettingsAsync(RepoSettings settings);
        Task<bool> DeleteRepoSettingsAsync(string ownerId, string repositoryId);
        Task<RepoSettings> GetRepoSettingsByIdAsync(string id);
    }
}
