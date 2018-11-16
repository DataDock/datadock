using DataDock.Common.Models;
using System.Security.Claims;
using System.Threading.Tasks;


namespace DataDock.Web.Services
{
    public interface IImportService
    {
        Task<RepoSettings> CheckRepoSettingsAsync(ClaimsPrincipal user, string ownerId, string repoId);
        Task<bool> CheckUserIsAdminOfOwner(ClaimsPrincipal user, string ownerId);
    }
}
