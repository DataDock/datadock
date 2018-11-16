using System.Threading.Tasks;
using DataDock.Common.Models;

namespace DataDock.Common.Stores
{
    public interface IOwnerSettingsStore
    {
        Task<OwnerSettings> GetOwnerSettingsAsync(string ownerId);

        Task CreateOrUpdateOwnerSettingsAsync(OwnerSettings ownerSettings);

        Task<bool> DeleteOwnerSettingsAsync(string ownerId);
    }
}
