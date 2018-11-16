using System;
using System.IO;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace DataDock.Worker.Processors
{
    public class ImportSchemaProcessor : IDataDockProcessor
    {
        private readonly ISchemaStore _schemaStore;
        private readonly IFileStore _jobFileStore;
        private IProgressLog _progressLog;

        public ImportSchemaProcessor(ISchemaStore schemaStore, IFileStore jobFileStore)
        {
            _schemaStore = schemaStore;
            _jobFileStore = jobFileStore;
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
    }
}
