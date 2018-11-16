using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DataDock.Common.Models;

namespace DataDock.Worker.Processors
{
    public abstract class BaseJobProcessor : IDataDockProcessor
    {
        public WorkerConfiguration Configuration { get; }
        public IProgressLog ProgressLog { get; }

        protected BaseJobProcessor(WorkerConfiguration configuration, IProgressLog progressLog)
        {
            Configuration = configuration;
            ProgressLog = progressLog;
        }

        public abstract Task ProcessJob(JobInfo jobInfo, UserAccount userInfo, IProgressLog progressLog);
    }
}
