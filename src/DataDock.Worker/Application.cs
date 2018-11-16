using System;
using System.Threading;
using System.Threading.Tasks;
using DataDock.Common;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Worker.Processors;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Serilog;
using VDS.RDF;

namespace DataDock.Worker
{
    public class Application
    {
        private IServiceProvider Services { get; }

        public Application(IServiceProvider services)
        {
            Services = services;
        }

        public async Task Run()
        {
            Log.Information("Worker application started");
            var jobRepo = Services.GetRequiredService<IJobStore>();
            while (true)
            {
                Thread.Sleep(1000);
                var job = await jobRepo.GetNextJob();
                if (job != null)
                {
                    Log.Information("Found new job: {JobId} {JobType}", job.JobId, job.JobType);
                    await ProcessJob(jobRepo, job);
                }
            }
        }

        private async Task ProcessJob(IJobStore jobStore, JobInfo jobInfo)
        {
            var progressLogFactory = Services.GetRequiredService<IProgressLogFactory>();
            var progressLog = await progressLogFactory.MakeProgressLogForJobAsync(jobInfo);
            var logStore = Services.GetService<ILogStore>();
            try
            {
                var jobLogger = Log.ForContext("JobId", jobInfo.JobId);
                jobLogger.Information("Processing Job {JobId} - {JobType}", jobInfo.JobId, jobInfo.JobType);

                jobLogger.Debug("Retrieving user account for {UserId}", jobInfo.UserId);
                var userRepo = Services.GetRequiredService<IUserStore>();
                var userAccount = await userRepo.GetUserAccountAsync(jobInfo.UserId);

                // TODO: Should encapsulate this logic plus basic job info validation into its own processor factory class (issue #83)
                jobLogger.Debug("Creating job processor for job type {JobType}", jobInfo.JobType);
                IDataDockProcessor processor;
                switch (jobInfo.JobType)
                {
                    case JobType.Import:
                    {
                        var cmdProcessorFactory = Services.GetRequiredService<IGitCommandProcessorFactory>();
                        processor = new ImportJobProcessor(
                            Services.GetRequiredService<WorkerConfiguration>(),
                            cmdProcessorFactory.MakeGitCommandProcessor(progressLog),
                            Services.GetRequiredService<IGitHubClientFactory>(),
                            Services.GetRequiredService<IDatasetStore>(),
                            Services.GetRequiredService<IFileStore>(),
                            Services.GetRequiredService<IOwnerSettingsStore>(),
                            Services.GetRequiredService<IRepoSettingsStore>(),
                            Services.GetRequiredService<IDataDockRepositoryFactory>(),
                            Services.GetRequiredService<IDataDockUriService>());
                        break;
                    }
                    case JobType.Delete:
                    {
                        var ddRepoFactory = Services.GetRequiredService<IDataDockRepositoryFactory>();
                        var cmdProcessorFactory = Services.GetRequiredService<IGitCommandProcessorFactory>();
                        processor = new DeleteDatasetProcessor(
                            Services.GetRequiredService<WorkerConfiguration>(),
                            cmdProcessorFactory.MakeGitCommandProcessor(progressLog),
                            Services.GetRequiredService<IDatasetStore>(),
                            ddRepoFactory.GetRepositoryForJob(jobInfo, progressLog));
                        break;
                    }
                    case JobType.SchemaCreate:
                        processor = new ImportSchemaProcessor(Services.GetRequiredService<ISchemaStore>(), Services.GetRequiredService<IFileStore>());
                        break;
                    case JobType.SchemaDelete:
                        processor = new DeleteSchemaProcessor(Services.GetRequiredService<ISchemaStore>());
                        break;
                    default:
                        throw new WorkerException($"Could not process job of type {jobInfo.JobType}");
                }

                // Log start
                jobLogger.Debug("Start job processor");
                jobInfo.StartedAt = DateTime.UtcNow;
                jobInfo.CurrentStatus = JobStatus.Running;
                await jobStore.UpdateJobInfoAsync(jobInfo);
                progressLog.UpdateStatus(JobStatus.Running, "Job processing started");

                await processor.ProcessJob(jobInfo, userAccount, progressLog);

                // Log end
                jobLogger.Information("Job processing completed");
                var logId = await logStore.AddLogAsync(jobInfo.OwnerId, jobInfo.RepositoryId, jobInfo.JobId,
                    progressLog.GetLogText());
                jobInfo.CurrentStatus = JobStatus.Completed;
                jobInfo.CompletedAt = DateTime.UtcNow;
                jobInfo.LogId = logId;
                await jobStore.UpdateJobInfoAsync(jobInfo);
                progressLog.UpdateStatus(jobInfo.CurrentStatus, "Job completed");
            }
            catch (Exception ex)
            {
                if (ex is WorkerException wex)
                {
                    progressLog.UpdateStatus(JobStatus.Failed, wex.Message);
                    Log.Error(wex, "WorkerException raised for job {JobId}", jobInfo.JobId);
                }
                else
                {
                    Log.Error(ex, "Job processing failed for job {JobId}", jobInfo.JobId);
                }

                var logId = await logStore.AddLogAsync(jobInfo.OwnerId, jobInfo.RepositoryId, jobInfo.JobId,
                    progressLog.GetLogText());
                jobInfo.LogId = logId;
                jobInfo.CurrentStatus = JobStatus.Failed;
                jobInfo.CompletedAt = DateTime.UtcNow;
                await jobStore.UpdateJobInfoAsync(jobInfo);
                progressLog.UpdateStatus(JobStatus.Failed, "Job processing failed");
            }
        }


        
    }
}
