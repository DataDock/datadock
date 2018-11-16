using DataDock.Common.Models;

namespace DataDock.Worker
{
    public interface IDataDockRepositoryFactory
    {
        IDataDockRepository GetRepositoryForJob(JobInfo jobInfo, IProgressLog progressLog);
    }
}