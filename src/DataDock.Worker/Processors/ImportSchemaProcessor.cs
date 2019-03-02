using System;
using System.IO;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace DataDock.Worker.Processors
{
    public class ImportSchemaProcessor : IDataDockProcessor
    {
        private readonly ISchemaStore _schemaStore;
        private readonly IFileStore _jobFileStore;
        private readonly WorkerConfiguration _configuration;
        private IProgressLog _progressLog;

        public ImportSchemaProcessor(WorkerConfiguration configuration, ISchemaStore schemaStore, IFileStore jobFileStore)
        {
            _schemaStore = schemaStore;
            _jobFileStore = jobFileStore;
            _configuration = configuration;
        }

        public async Task ProcessJob(JobInfo job, UserAccount userAccount, IProgressLog progressLog)
        {
            _progressLog = progressLog;
            // Save the schema to documentDB
            try
            {
                Log.Debug("Create schema. Schema file Id: {schemaFileId}", job.SchemaFileId);
                _progressLog.UpdateStatus(JobStatus.Running, "Create schema");
                // get schema from file store
                if (!string.IsNullOrEmpty(job.SchemaFileId))
                {
                    // Parse the JSON metadata
                    JObject schemaJson;
                    var schemaFileStream = await _jobFileStore.GetFileAsync(job.SchemaFileId);

                    using (var sr = new StreamReader(schemaFileStream))
                    {
                        using (var jr = new JsonTextReader(sr))
                        {
                            schemaJson = JObject.Load(jr);
                        }
                    }
                    if (schemaJson != null)
                    {
                        _progressLog.UpdateStatus(JobStatus.Running, "Retrieved DataDock schema file.");

                        MakeRelative(schemaJson,
                            $"{_configuration.PublishUrl}{(_configuration.PublishUrl.EndsWith("/") ? string.Empty : "/")}{job.OwnerId}/{job.RepositoryId}/");

                        Log.Debug("Create schema: OwnerId: {ownerId} RepositoryId: {repoId} SchemaFileId: {schemaFileId}",
                            job.OwnerId, job.RepositoryId, job.SchemaFileId);

                        var schemaInfo = new SchemaInfo
                        {
                            OwnerId = job.OwnerId,
                            RepositoryId = job.RepositoryId,
                            LastModified = DateTime.UtcNow,
                            SchemaId = Guid.NewGuid().ToString(),
                            Schema = schemaJson,
                        };
                        _progressLog.UpdateStatus(JobStatus.Running, "Creating schema record.");

                        await _schemaStore.CreateOrUpdateSchemaRecordAsync(schemaInfo);
                        _progressLog.UpdateStatus(JobStatus.Running, "Schema record created successfully.");
                    }
                    else
                    {
                        _progressLog.UpdateStatus(JobStatus.Failed,
                            "Unable to create schema - unable to retrieve schema JSON from temporary file storage");
                        throw new WorkerException(
                            "Unable to create schema - unable to retrieve schema JSON from temporary file storage");
                    }
                }
                else
                {
                    _progressLog.UpdateStatus(JobStatus.Failed, "Unable to create schema - missing file Id");
                    throw new WorkerException("Unable to create schema - missing file Id");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update schema record");
                _progressLog.UpdateStatus(JobStatus.Failed, "Failed to update schema record");

                throw new WorkerException(ex,
                    "Failed to update schema record.");
            }
        }

        private void MakeRelative(JToken tok, string baseUri)
        {
            switch (tok)
            {
                case JObject o:
                {
                    foreach (var p in o.Properties())
                    {
                        if (p.Name.Equals("aboutUrl") ||
                            p.Name.Equals("propertyUrl") ||
                            p.Name.Equals("valueUrl"))
                        {
                            if (p.Value.Type == JTokenType.String &&
                                p.Value.Value<string>().StartsWith(baseUri))
                            {
                                p.Value = p.Value.Value<string>().Substring(baseUri.Length);
                            }
                        }
                        if (p.Value.Type == JTokenType.Array || p.Value.Type == JTokenType.Object)
                        {
                            MakeRelative(p.Value, baseUri);
                        }
                    }

                    break;
                }
                case JArray a:
                {
                    foreach (var item in a) MakeRelative(item, baseUri);
                    break;
                }
            }
        }
    }
}
