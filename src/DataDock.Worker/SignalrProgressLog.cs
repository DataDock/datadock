using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace DataDock.Worker
{
    public class SignalrProgressLog : IProgressLog
    {
        private readonly JobInfo _jobInfo;
        private readonly IJobStore _jobRepository;
        private readonly HubConnection _hubConnection;
        private readonly ILogger _log;
        private readonly StringBuilder _fullLog;

        public SignalrProgressLog(JobInfo jobInfo, IJobStore jobRepository, HubConnection hubConnection)
        {
            _jobInfo = jobInfo;
            _jobRepository = jobRepository;
            _hubConnection = hubConnection;
            _log = Log.ForContext("JobId", jobInfo.JobId);
            _fullLog = new StringBuilder();
        }

       


        public void UpdateStatus(JobStatus newStatus, string progressMessage, params object[] args)
        {
            var logMessage = FormatMessage(newStatus == JobStatus.Failed ? "ERROR" : "INFO", progressMessage, args);
            _jobInfo.CurrentStatus = newStatus;
            _jobRepository.UpdateJobInfoAsync(_jobInfo);
            NotifyStatusAsync(newStatus);
            NotifyAsync(logMessage);
            if (newStatus == JobStatus.Failed)
            {
                _log.Error(logMessage);
            }
            else
            {
                _log.Information(logMessage);
            }
            _fullLog.AppendLine(logMessage);
        }

        public void Info(string infoMessage, params object[] args)
        {
            var logMessage = FormatMessage("INFO", infoMessage, args);
            UpdateJobAsync();
            NotifyAsync(logMessage);
            _log.Information(logMessage);
            _fullLog.AppendLine(logMessage);
        }

        public void Warn(string warnMessage, params object[] args)
        {
            var logMessage = FormatMessage("WARN", warnMessage, args);
            UpdateJobAsync();
            NotifyAsync(logMessage);
            _log.Warning(logMessage);
            _fullLog.AppendLine(logMessage);
        }

        public void Error(string errorMessage, params object[] args)
        {
            var logMessage = FormatMessage("ERROR", errorMessage, args);
            UpdateJobAsync();
            NotifyAsync(logMessage);
            _log.Error(logMessage);
            _fullLog.AppendLine(logMessage);
        }

        public void Exception(Exception exception, string errorMessage, params object[] args)
        {
            var logMessage = FormatMessage("ERROR", errorMessage, args);
            UpdateJobAsync();
            NotifyAsync(logMessage);
            _log.Error(exception, logMessage);
            _fullLog.AppendLine(logMessage);
            _fullLog.AppendLine(exception.ToString());
        }

        public string GetLogText()
        {
            return _fullLog.ToString();
        }

        private static string FormatMessage(string level, string fmt, object[] args)
        {
            return $"[{DateTime.UtcNow:u}] - {level} - {string.Format(fmt, args)}";
        }

        private void UpdateJobAsync()
        {
            try
            {
                _jobRepository.UpdateJobInfoAsync(_jobInfo);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error updating job");
            }
        }

        private void NotifyStatusAsync(JobStatus jobStatus)
        {
            try
            {
                
                _hubConnection.InvokeAsync("StatusUpdated", _jobInfo.UserId, _jobInfo.JobId, jobStatus);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error notifying SignalR hub StatusUpdated method");
            }
        }

        private void NotifyAsync(string message)
        {
            try
            {
                _hubConnection.InvokeAsync("ProgressUpdated", _jobInfo.UserId, _jobInfo.JobId, message);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error notifying SignalR hub ProgressUpdated method");
            }
        }
    }
}
