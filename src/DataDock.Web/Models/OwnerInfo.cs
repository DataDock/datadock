using System.Collections.Generic;
using Newtonsoft.Json;

namespace DataDock.Web.Models
{
    public class OwnerInfo
    {
        [JsonProperty("ownerId")]
        public string OwnerId { get; set; }
        [JsonProperty("avatarUrl")]
        public string AvatarUrl { get; set; }
        [JsonProperty("repositories")]
        public List<RepositoryInfo> Repositories { get; set; }

        public OwnerInfo()
        {
            Repositories = new List<RepositoryInfo>();
        }
    }
}
