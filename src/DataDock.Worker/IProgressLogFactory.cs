using System.Threading.Tasks;
using DataDock.Common.Models;

namespace DataDock.Worker
{
    public interface IProgressLogFactory
    {
        Task<IProgressLog> MakeProgressLogForJobAsync(JobInfo job);
    }
}
