using DataDock.Common.Models;
using Octokit;

namespace DataDock.Web.Models
{
    public class RepositoryInfo
    {
        public string OwnerId { get; set; }
        public string RepoId { get; set; }
        public string DataDockImportUrl { get; set; }
        public string OwnerAvatar { get; set; }

        public RepositoryInfo()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r">A GitHub repo <see cref="Repository"/></param>
        /// <param name="schemaId">optional schemaId to add to the import URL</param>
        public RepositoryInfo(Repository r, string schemaId = "")
        {
            this.OwnerId = r.Owner?.Login;
            this.RepoId = r.Name;
            this.DataDockImportUrl = $"/dashboard/import/{OwnerId}/{RepoId}";
            if (!string.IsNullOrEmpty(schemaId))
            {
                this.DataDockImportUrl += $"/{schemaId}";
            }
            this.OwnerAvatar = r.Owner?.AvatarUrl;
        }

        public RepositoryInfo(RepoSettings s)
        {
            this.OwnerId = s.OwnerId;
            this.RepoId = s.RepositoryId;
            this.DataDockImportUrl = $"/dashboard/import/{OwnerId}/{RepoId}";
            this.OwnerAvatar = s.OwnerAvatar;
        }
    }
}
