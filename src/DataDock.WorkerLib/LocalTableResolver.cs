using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DataDock.CsvWeb;
using Newtonsoft.Json.Linq;

namespace DataDock.Worker
{
    public class LocalTableResolver : ITableResolver
    {
        private readonly Dictionary<Uri, string> _lookup = new Dictionary<Uri, string>();
        private readonly Uri _baseUri;
        private readonly Uri _localBaseUri;

        /// <summary>
        /// Create a new resolver which resolves table URIs relative to the specified metadata file base URI
        /// </summary>
        /// <param name="metadataUri">The URI of the metadata file</param>
        /// <param name="metadataFilePath">The path to the local instance of the metadata file</param>
        public LocalTableResolver(Uri metadataUri, string metadataFilePath)
        {
            _baseUri = metadataUri;
            if (!Uri.TryCreate(Path.GetFullPath(metadataFilePath), UriKind.Absolute, out _localBaseUri) || _localBaseUri.Scheme != "file")
            {
                throw new ArgumentException("Unable to create a file:// URI from the provided local file path",
                    nameof(metadataFilePath));
            }
        }

        /// <summary>
        /// Add a resolved file path to the local cache
        /// </summary>
        /// <remarks>This overrides any relative file path resolution for the specified URI.</remarks>
        /// <param name="csvUri">The CSV lookup URI</param>
        /// <param name="csvFilePath">The resolved local file path to return</param>
        public void CacheResolvedUri(Uri csvUri, string csvFilePath)
        {
            _lookup[csvUri] = csvFilePath;
        }

        public async Task<Stream> ResolveAsync(Uri tableUri)
        {
            return await Task.Run(() =>
            {
                if (_lookup.TryGetValue(tableUri, out var filePath))
                {
                    return File.OpenRead(filePath);
                }

                var relPath = tableUri.MakeRelativeUri(_baseUri);
                var localUri = new Uri(_localBaseUri, relPath);
                var localPath = localUri.AbsoluteUri;
                if (!File.Exists(localPath))
                {
                    throw new FileNotFoundException(
                        $"Resolved Table URI {tableUri} to local URI {localUri}, but no file was found at that location.");
                }
                CacheResolvedUri(tableUri, localPath);
                    return File.OpenRead(localUri.AbsoluteUri);
            });
        }

        public Task<JObject> ResolveJsonAsync(Uri jsonUri)
        {
            throw new NotImplementedException();
        }
    }
}
