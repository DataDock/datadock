using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataDock.Worker
{
    public interface IResourceFileMapper
    {
        /// <summary>
        /// Returns the mapped output path for the specified resource URI
        /// </summary>
        /// <param name="resourceUri">The resource URI</param>
        /// <returns>The mapped output path for the resource, or null if no mapping was found</returns>
        string GetPathFor(Uri resourceUri);

        /// <summary>
        /// Determines if a resource URI has an output mapping
        /// </summary>
        /// <param name="resourceUri">The resource URI</param>
        /// <returns>True if the resource URI can be mapped to an output path, false otherwise</returns>
        bool CanMap(Uri resourceUri);

        /// <summary>
        /// Returns an enumeration over the unique output paths mapped by this file mapper, optionally combined with a base path
        /// </summary>
        /// <param name="basePath">OPTIONAL: The base path to be used to resolve the map entry to the actual output path</param>
        /// <returns></returns>
        IEnumerable<string> GetMappedPaths(string basePath=null);
    }

    public class ResourceFileMapper : IResourceFileMapper
    {
        private readonly List<ResourceMapEntry> _mapEntries;

        public ResourceFileMapper(params ResourceMapEntry[] entries)
        {
            _mapEntries = new List<ResourceMapEntry>(entries);
        }

        public ResourceFileMapper(List<ResourceMapEntry> entries)
        {
            _mapEntries = entries;
        }

        public string GetPathFor(Uri resourceUri)
        {
            if (resourceUri == null) return null;
            foreach (var entry in _mapEntries)
            {
                if (entry.BaseUri.IsBaseOf(resourceUri))
                {
                    var subjectRel = entry.BaseUri.MakeRelativeUri(resourceUri).ToString();
                    subjectRel = Uri.UnescapeDataString(subjectRel);
                    var subjectPath = Path.Combine(subjectRel.Split('/'));
                    if (string.Empty.Equals(subjectRel) || subjectRel.EndsWith("/"))
                    {
                        subjectPath = Path.Combine(subjectPath, "index");
                    }
                    return Path.Combine(entry.OutputDirectoryPath, subjectPath);
                }
            }
            return null;
        }

        public bool CanMap(Uri resourceUri)
        {
            return resourceUri != null && _mapEntries.Any(m => m.BaseUri.IsBaseOf(resourceUri));
        }

        public IEnumerable<string> GetMappedPaths(string basePath = null)
        {
            return _mapEntries
                .Select(m => m.OutputDirectoryPath)
                .Distinct()
                .Select(x => basePath == null ? x : Path.Combine(basePath, x));
        }
    }

    public class ResourceMapEntry
    {
        public Uri BaseUri { get; }
        public string OutputDirectoryPath { get; }

        public ResourceMapEntry(Uri baseUri, string outputDirectoryPath)
        {
            BaseUri = baseUri;
            OutputDirectoryPath = outputDirectoryPath;
        }
    }

}
