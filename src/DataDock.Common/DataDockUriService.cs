using System;
using System.Text.RegularExpressions;
using FluentValidation.Validators;

namespace DataDock.Common
{
    public class DataDockUriService : IDataDockUriService
    {
        public readonly string PublishSite;
        public readonly Regex IdentifierRegex;

        public DataDockUriService(string publishSiteBaseUri)
        {
            if (string.IsNullOrEmpty(publishSiteBaseUri)) throw new ArgumentException("Base URI must be a non-null non-empty string", nameof(publishSiteBaseUri));
            PublishSite = publishSiteBaseUri + (publishSiteBaseUri.EndsWith('/') ? string.Empty : "/");
            IdentifierRegex  = new Regex("^"  + Regex.Escape(publishSiteBaseUri) + "([^/]+)/([^/]+)/id/(.*)");
        }

        public string GetBaseUri() => PublishSite;

        public string GetRepositoryUri(string ownerId, string repositoryId)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentException("Identifier must be a non-null non-empty string", nameof(ownerId));
            if (string.IsNullOrEmpty(repositoryId)) throw new ArgumentException("Identifier must be a non-null non-empty string", nameof(repositoryId));
            return $"{PublishSite}{ownerId}/{repositoryId}/";
        }

        public string GetIdentifierPrefix(string ownerId, string repositoryId)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentException("Identifier must be a non-null non-empty string", nameof(ownerId));
            if (string.IsNullOrEmpty(repositoryId)) throw new ArgumentException("Identifier must be a non-null non-empty string", nameof(repositoryId));
            return $"{PublishSite}{ownerId}/{repositoryId}/id/";
        }

        public string GetDatasetIdentifier(string ownerId, string repositoryId, string datasetId)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentException("Identifier must be a non-null non-empty string", nameof(ownerId));
            if (string.IsNullOrEmpty(repositoryId)) throw new ArgumentException("Identifier must be a non-null non-empty string", nameof(repositoryId));
            if (string.IsNullOrEmpty(datasetId)) throw new ArgumentException("Identifier must be a non-null non-empty string", nameof(datasetId));
            return $"{PublishSite}{ownerId}/{repositoryId}/id/dataset/{datasetId}";
        }

        public string GetRepositoryPublisherIdentifier(string ownerId, string repositoryId)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentException("Identifier must be a non-null non-empty string", nameof(ownerId));
            if (string.IsNullOrEmpty(repositoryId)) throw new ArgumentException("Identifier must be a non-null non-empty string", nameof(repositoryId));
            return $"{PublishSite}{ownerId}/{repositoryId}/id/dataset/publisher";
        }

        public string GetMetadataGraphIdentifier(string ownerId, string repositoryId)
        {
            return GetRepositoryUri(ownerId, repositoryId) + "metadata";
        }

        public string GetDefinitionsGraphIdentifier(string ownerId, string repositoryId)
        {
            return GetRepositoryUri(ownerId, repositoryId) + "definitions";
        }

        public string GetSubjectDataUrl(string subjectIdentifier, string fileExtension)
        {
            if(string.IsNullOrEmpty(subjectIdentifier)) throw new ArgumentException("Identifier must be a non-null non-empty string", nameof(subjectIdentifier));
            return IdentifierRegex.Replace(subjectIdentifier, PublishSite + "$1/$2/data/$3") +
                   (fileExtension != null ? "." + fileExtension : string.Empty);
        }
    }
}
