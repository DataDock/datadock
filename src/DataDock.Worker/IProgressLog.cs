using System;
using DataDock.Common.Models;

namespace DataDock.Worker
{
    public interface IProgressLog
    {
        /// <summary>
        /// Update the running status of a job and log an informational message
        /// </summary>
        /// <param name="newStatus"></param>
        /// <param name="progressMessage"></param>
        /// <param name="args"></param>
        void UpdateStatus(JobStatus newStatus, string progressMessage, params object[] args);

        /// <summary>
        /// Log an informational message
        /// </summary>
        /// <param name="infoMessage"></param>
        /// <param name="args"></param>
        void Info(string infoMessage, params object[] args);

        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="warnMessage"></param>
        /// <param name="args"></param>
        void Warn(string warnMessage, params object[] args);

        /// <summary>
        /// Log an error message
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="args"></param>
        void Error(string errorMessage, params object[] args);

        /// <summary>
        /// Log an error message with additional exception detail
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="errorMessage"></param>
        /// <param name="args"></param>
        void Exception(Exception exception, string errorMessage, params object[] args);

        /// <summary>
        /// Get the full text of the log so far
        /// </summary>
        /// <returns></returns>
        string GetLogText();
    }
}
