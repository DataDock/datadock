namespace DataDock.Common
{
    /// <summary>
    /// Implements the <see cref="IRepositoryUriService"/> strategy for the DataDock service
    /// </summary>
    public class DataDockRepositoryUriService : IRepositoryUriService
    {
        private readonly string _repositoryUri;

        public DataDockRepositoryUriService(string repositoryUri)
        {
            _repositoryUri = repositoryUri;
            if (!_repositoryUri.EndsWith("/")) _repositoryUri += "/";
        }

        /// <inheritdoc />
        public string IdentifierPrefix => $"{_repositoryUri}id/";

        public string PagePrefix => $"{_repositoryUri}page/";

        /// <inheritdoc />
        public string GetDatasetIdentifier(string datasetId)
        {
            return $"{IdentifierPrefix}dataset/{datasetId}";
        }

        /// <inheritdoc />
        public string GetDatasetMetadataIdentifier(string datasetId)
        {
            return $"{GetDatasetIdentifier(datasetId)}/metadata";
        }

        /// <inheritdoc />
        public string RepositoryPublisherIdentifier => $"{IdentifierPrefix}publisher";

        /// <inheritdoc />
        public string MetadataGraphIdentifier => $"{_repositoryUri}metadata";

        /// <inheritdoc />
        public string DefinitionsGraphIdentifier => $"{_repositoryUri}definitions";

        
    }
}