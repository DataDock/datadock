using DataDock.Common.Models;
using DataDock.Worker;

namespace DataDock.Command
{
    internal class ConsoleProgressLog : IProgressLog
    {
        /// <inheritdoc />
        public void UpdateStatus(JobStatus newStatus, string progressMessage, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void DatasetUpdated(DatasetInfo datasetInfo)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void DatasetDeleted(string ownerId, string repoId, string datasetId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Info(string infoMessage, params object[] args)
        {
            Console.WriteLine($"[INFO] - {string.Format(infoMessage, args)}");
        }

        /// <inheritdoc />
        public void Warn(string warnMessage, params object[] args)
        {
            Console.WriteLine($"[WARN] - {string.Format(warnMessage, args)}");
        }

        /// <inheritdoc />
        public void Error(string errorMessage, params object[] args)
        {
            Console.WriteLine($"[ERROR] - {string.Format(errorMessage, args)}");
        }

        /// <inheritdoc />
        public void Exception(Exception exception, string errorMessage, params object[] args)
        {
            Console.WriteLine($"[FATAL] - {string.Format(errorMessage, args)}");
            Console.WriteLine(exception);
        }

        /// <inheritdoc />
        public string GetLogText()
        {
            throw new NotImplementedException();
        }
    }
}
