using System;
using System.Collections.Generic;
using System.Text;

namespace DataDock.Common
{
    /// <summary>
    /// Defines the service interface for a helper class that knows the URI patterns used in the DataDock service
    /// </summary>
    public interface IDataDockUriService
    {
        /// <summary>
        /// Returns the base URI used for all generated URIs for the DataDock service
        /// </summary>
        /// <returns></returns>
        string GetBaseUri();

        /// <summary>
        /// Returns the URL for a DataDock data repository
        /// </summary>
        /// <param name="ownerId">The GitHub owner ID</param>
        /// <param name="repositoryId">The GitHub repository ID</param>
        /// <returns>The DataDock service URL for the data repository</returns>
        string GetRepositoryUri(string ownerId, string repositoryId);

        /// <summary>
        /// Returns the default URI prefix used for resources in the specified DataDock data repository
        /// </summary>
        /// <param name="ownerId">The GitHub owner ID</param>
        /// <param name="repositoryId">The GitHub repository ID</param>
        /// <returns>The URI prefix used for resource identifiers in the specified repository. The returned URI ends with a slash so that identifiers can be constructed by simple string concatenation.</returns>
        string GetIdentifierPrefix(string ownerId, string repositoryId);

        /// <summary>
        /// Returns the default URI for a single dataset in a DataDock data repository
        /// </summary>
        /// <param name="ownerId">The GitHub owner ID</param>
        /// <param name="repositoryId">The GitHub repository ID</param>
        /// <param name="datasetId">The dataset identifier</param>
        /// <returns>The URI that identifies the specified dataset.</returns>
        string GetDatasetIdentifier(string ownerId, string repositoryId, string datasetId);

        /// <summary>
        /// Returns the default URI for a dataset repository publisher
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repositoryId"></param>
        /// <returns></returns>
        string GetRepositoryPublisherIdentifier(string ownerId, string repositoryId);

        /// <summary>
        /// Returns the default URI for a dataset repository's metadata graph
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repositoryId"></param>
        /// <returns></returns>
        string GetMetadataGraphIdentifier(string ownerId, string repositoryId);

        /// <summary>
        /// Returns the default URI for a dataset repository's definitions graph
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repositoryId"></param>
        /// <returns></returns>
        string GetDefinitionsGraphIdentifier(string ownerId, string repositoryId);

        /// <summary>
        /// Generates a data resource URL for a subject from the subject IRI
        /// </summary>
        /// <param name="subjectIdentifier">The subject IRI</param>
        /// <param name="fileExtension">The file extension to append to the data resource URL</param>
        /// <returns>The URL to retrieve the RDF data for a subject resource</returns>
        string GetSubjectDataUrl(string subjectIdentifier, string fileExtension);

    }
}
