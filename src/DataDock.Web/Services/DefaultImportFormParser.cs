using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Web.Api;
using DataDock.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace DataDock.Web.Services
{
    public class DefaultImportFormParser : IImportFormParser
    {
        private readonly IFileStore _fileStore;

        public DefaultImportFormParser(IFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public async Task<ImportFormParserResult> ParseImportFormAsync(HttpRequest request, string userId,
            Func<ImportFormData, IFormCollection, Task<bool>> bindAsyncFunc)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (userId == null) throw new ArgumentNullException(nameof(userId));
            if (bindAsyncFunc ==null) throw new ArgumentNullException(nameof(bindAsyncFunc));
            if (!MultipartRequestHelper.IsMultipartContentType(request.ContentType))
            {
                return new ImportFormParserResult($"Expected a multipart request, but got {request.ContentType}");
            }

            var defaultFormOptions = new FormOptions();

            var jobInfo = new ImportJobRequestInfo
            {
                UserId = userId
            };

            var formAccumulator = new KeyValueAccumulator();

            var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(request.ContentType),
                defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, request.Body);
            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        jobInfo.CsvFileName = string.Empty;
                        jobInfo.DatasetId = string.Empty;
                        Log.Information("api/data(POST): Starting conversion job. UserId='{0}', File='{1}'", userId,
                            "");
                        var csvFileId = await _fileStore.AddFileAsync(section.Body);

                        Log.Information($"Saved the uploaded CSV file '{csvFileId}'");
                        jobInfo.CsvFileId = csvFileId;
                    }
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                        var encoding = GetEncoding(section);
                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 1024,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();
                            if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = String.Empty;
                            }

                            formAccumulator.Append(key.ToString(), value);

                            if (formAccumulator.ValueCount > defaultFormOptions.ValueCountLimit)
                            {
                                throw new InvalidDataException(
                                    $"Form key count limit {defaultFormOptions.ValueCountLimit} exceeded.");
                            }
                        }
                    }
                }

                // Drains any remaining section body that has not been consumed and
                // reads the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }

            // Bind form data to a model
            var formData = new ImportFormData();
            var bindingSuccessful = await bindAsyncFunc(formData, new FormCollection(formAccumulator.GetResults()));
            if (!bindingSuccessful) return new ImportFormParserResult("Import form data validation failed");

            jobInfo.OwnerId = formData.OwnerId;
            jobInfo.RepositoryId = formData.RepoId;
            if (string.IsNullOrEmpty(formData.OwnerId) || string.IsNullOrEmpty(formData.RepoId))
            {
                Log.Error("DataController: POST called with no owner or repo set in FormData");
                return new ImportFormParserResult("No target repository supplied");
            }

            if (formData.Metadata == null)
            {
                Log.Error("DataController: POST called with no metadata present in FormData");
                return new ImportFormParserResult("No metadata supplied");
            }

            var parser = new JsonSerializer();
            Log.Debug("DataController: Metadata: {0}", formData.Metadata);
            var metadataObject = parser.Deserialize(new JsonTextReader(new StringReader(formData.Metadata))) as JObject;
            if (metadataObject == null)
            {
                Log.Error(
                    "DataController: Error deserializing metadata as object, unable to create conversion job. Metadata = '{0}'",
                    formData.Metadata);
                return new ImportFormParserResult("Metadata badly formatted");
            }

            var datasetIri = metadataObject["url"]?.ToString();
            var datasetTitle = metadataObject["dc:title"]?.ToString();
            if (string.IsNullOrEmpty(datasetIri))
            {
                Log.Error("DataController: No dataset IRI supplied in metadata.");
                return new ImportFormParserResult("No dataset IRI supplied in metadata");
            }

            var datasetId = formData.Filename;
            var datasetIriSplit = datasetIri.Split("/");
            if (datasetIriSplit != null && datasetIriSplit.Length > 1)
            {
                datasetId = datasetIriSplit[datasetIriSplit.Length - 1];
            }

            Log.Debug("DataController: datasetIri = '{0}'", datasetIri);
            jobInfo.DatasetIri = datasetIri;

            // save CSVW to file storage
            if (string.IsNullOrEmpty(jobInfo.CsvFileName))
            {
                jobInfo.CsvFileName = formData.Filename;
                jobInfo.DatasetId = datasetId;
            }
            jobInfo.IsPublic = formData.ShowOnHomePage;

            byte[] byteArray = Encoding.UTF8.GetBytes(formData.Metadata);
            var metadataStream = new MemoryStream(byteArray);
            var csvwFileId = await _fileStore.AddFileAsync(metadataStream);
            jobInfo.CsvmFileId = csvwFileId;

            if (formData.SaveAsSchema)
            {
                Log.Information("api/data(POST): Saving metadata as template.");

                var schema = new JObject(new JProperty("dc:title", "Template from " + datasetTitle),
                    new JProperty("metadata", metadataObject));
                Log.Information("api/data(POST): Starting schema creation job. UserId='{0}', Repository='{1}'", userId,
                    formData.RepoId);

                byte[] schemaByteArray = Encoding.UTF8.GetBytes(schema.ToString());
                var schemaStream = new MemoryStream(schemaByteArray);
                var schemaFileId = await _fileStore.AddFileAsync(schemaStream);

                if (!string.IsNullOrEmpty(schemaFileId))
                {
                    Log.Information("api/data(POST): Schema temp file saved: {0}.", schemaFileId);
                    var schemaJobRequest = new SchemaImportJobRequestInfo()
                    {
                        UserId = userId,
                        SchemaFileId = schemaFileId,
                        OwnerId = formData.OwnerId,
                        RepositoryId = formData.RepoId
                    };
                    return new ImportFormParserResult(jobInfo, schemaJobRequest, formData.Metadata);
                }

                Log.Error(
                    "api/data(POST): Error saving schema content to temporary file storage, unable to start schema creation job");
            }

            return new ImportFormParserResult(jobInfo, formData.Metadata);
    }

        private static Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in 
            // most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }

    }
}
