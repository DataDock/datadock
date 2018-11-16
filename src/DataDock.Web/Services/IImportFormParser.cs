using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Web.Models;
using Microsoft.AspNetCore.Http;

namespace DataDock.Web.Services
{
    /// <summary>
    /// Defines the interface for a service that parses an import job request from an HTTP POST
    /// </summary>
    public interface IImportFormParser
    {
        Task<ImportFormParserResult> ParseImportFormAsync(HttpRequest request, string userId, Func<ImportFormData, IFormCollection, Task<bool>> bindAsyncFunc);
    }

    public class ImportFormParserResult
    {
        public ImportJobRequestInfo ImportJobRequest { get; }
        public SchemaImportJobRequestInfo SchemaImportJobRequest { get; }
        public string Metadata { get; }
        public IList<string> ValidationErrors { get; }
        public bool IsValid { get; }

        public ImportFormParserResult(params string[] validationErrors)
        {
            IsValid = false;
            ValidationErrors = new List<string>(validationErrors);
        }

        public ImportFormParserResult(ImportJobRequestInfo importJob, SchemaImportJobRequestInfo schemaImportJob, string metadata)
        {
            IsValid = true;
            ValidationErrors = new List<string>(0);
            ImportJobRequest = importJob;
            SchemaImportJobRequest = schemaImportJob;
            Metadata = metadata;
        }

        public ImportFormParserResult(ImportJobRequestInfo importJob, string metadata):this(importJob, null, metadata)
        {
        }

    }
}
