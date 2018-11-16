using System.Threading.Tasks;

namespace DataDock.Common.Stores
{
    /// <summary>
    /// Interface for a persistent job log store
    /// </summary>
    public interface ILogStore
    {
        /// <summary>
        /// Add a log record to the store
        /// </summary>
        /// <param name="ownerId">The ID of the owner of the repository that the log refers to</param>
        /// <param name="repoId">The ID of the repository that the log refers to</param>
        /// <param name="jobId">The ID of the job that the log refers to</param>
        /// <param name="logText">The full log content</param>
        /// <returns>An identifier that can be used to retrieve the persistent log</returns>
        Task<string> AddLogAsync(string ownerId, string repoId, string jobId, string logText);

        /// <summary>
        /// Retrieves the content of a log
        /// </summary>
        /// <param name="logIdentifier">The log identifier that was generated when <see cref="AddLog"/> was called</param>
        /// <returns>The full log content</returns>
        /// <exception cref="LogNotFoundException">Raised if no log was found for the log identifier</exception>
        Task<string> GetLogContentAsync(string logIdentifier);

        /// <summary>
        /// Prune old logs from the store. The <see cref="LogTimeToLive"/> value will be used to determine old logs that can be removed
        /// </summary>
        void PruneLogs();

        /// <summary>
        /// Get the minimum number of days that a log will be kept in this log store
        /// </summary>
        int LogTimeToLive { get; }
    }

   
}
