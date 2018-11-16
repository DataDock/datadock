using System;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using Serilog;

namespace DataDock.Worker.Processors
{
    public class DeleteSchemaProcessor : IDataDockProcessor
    {
        private readonly ISchemaStore _schemaStore;

        public DeleteSchemaProcessor(ISchemaStore schemaStore)
        {
            _schemaStore = schemaStore;
        }

        public async Task ProcessJob(JobInfo job, UserAccount userAccount, IProgressLog progressLog)
        {
            // Delete the schema from documentDB
            try
            {
                progressLog.UpdateStatus(JobStatus.Running, $"Deleting schema {job.SchemaId}");
                await _schemaStore.DeleteSchemaAsync(null, job.SchemaId);
                progressLog.UpdateStatus(JobStatus.Running, "Schema deleted successfully");
            }
            catch (Exception ex)
            {
                progressLog.Error("Failed to remove schema record");
                Log.Error(ex, "Failed to remove schema record");
                throw new WorkerException(ex, "Failed to delete schema record.");
            }
        }
    }
}
