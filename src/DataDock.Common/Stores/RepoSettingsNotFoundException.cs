namespace DataDock.Common.Stores
{
    public class RepoSettingsNotFoundException : JobStoreException
    {
        public RepoSettingsNotFoundException(string ownerId) : base($"No repo settings found with ownerId {ownerId}") { }
        public RepoSettingsNotFoundException(string ownerId, string repositoryId) : base($"No repo settings found with ownerId {ownerId} and repositoryId {repositoryId}") { }
    }
}