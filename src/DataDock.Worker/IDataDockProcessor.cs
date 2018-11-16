using System.Threading.Tasks;
using DataDock.Common.Models;

namespace DataDock.Worker
{
    public interface IDataDockProcessor
    {
        Task ProcessJob(JobInfo jobInfo, UserAccount userInfo, IProgressLog progressLog);
    }
}
