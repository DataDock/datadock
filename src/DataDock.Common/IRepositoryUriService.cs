namespace DataDock.Common
{
    /// <summary>
    /// An interface for a strategy that generates URIs for items in a DataDock data repository
    /// </summary>
    public interface IRepositoryUriService
    {
        /// <summary>
        /// Returns the default URI prefix used for resources in the specified DataDock data repository
        /// </summary>
        /// <returns>The URI prefix used for resource identifiers in the specified repository. The returned URI ends with a slash so that identifiers can be constructed by simple string concatenation.</returns>
        string IdentifierPrefix { get; }

        /// <summary>
        /// Returns the default URI for a single dataset in a DataDock data repository
        /// </summary>
        /// <param name="datasetId">The dataset identifier</param>
        /// <returns>The URI that identifies the specified dataset.</returns>
        string GetDatasetIdentifier(string datasetId);

        string GetDatasetMetadataIdentifier(string datasetId);

        /// <summary>
        /// Returns the default URI for a dataset repository publisher
        /// </summary>
        /// <returns></returns>
        string RepositoryPublisherIdentifier { get; }

        /// <summary>
        /// Returns the default URI for a dataset repository's metadata graph
        /// </summary>
        /// <returns></returns>
        string MetadataGraphIdentifier { get; }

        /// <summary>
        /// Returns the default URI for a dataset repository's definitions graph
        /// </summary>
        /// <returns></returns>
        string DefinitionsGraphIdentifier { get; }

    };
}
