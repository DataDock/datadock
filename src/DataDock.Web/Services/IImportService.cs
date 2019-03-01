using DataDock.Common.Models;
using System.Security.Claims;
using System.Threading.Tasks;


namespace DataDock.Web.Services
{
    public interface IImportService
    {
        Task<OwnerSettings> CheckOwnerSettingsAsync(ClaimsPrincipal user, string ownerId);
        Task<RepoSettings> CheckRepoSettingsAsync(ClaimsPrincipal user, string ownerId, string repoId);
        Task<bool> CheckUserIsAdminOfOwner(ClaimsPrincipal user, string ownerId);
    }
}
