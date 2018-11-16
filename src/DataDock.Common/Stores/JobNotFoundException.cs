namespace DataDock.Common.Stores
{
    public class JobNotFoundException : JobStoreException
    {
        public JobNotFoundException(string jobId) : base("No job found for id " + jobId) { }
        public JobNotFoundException(string ownerId, string repositoryId) : base($"No jobs found for ownerId {ownerId} and repositoryId {repositoryId}") { }
    }
}