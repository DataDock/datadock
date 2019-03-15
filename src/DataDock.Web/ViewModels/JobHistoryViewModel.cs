using DataDock.Common.Models;
using System;

namespace DataDock.Web.ViewModels
{
    public class JobHistoryViewModel : DashboardViewModel
    {
        private readonly JobInfo _jobInfo;

        public JobHistoryViewModel(JobInfo jobInfo)
        {
            _jobInfo = jobInfo;
        }

        public string StatusClass
        {
            get
            {
                switch (_jobInfo.CurrentStatus)
                {
                    case JobStatus.Completed:
                        return "positive";
                    case JobStatus.Failed:
                        return "negative";
                    case JobStatus.Queued:
                        return "info";
                    case JobStatus.Running:
                        return "warning";
                    default:
                        return "info";
                }
            }
        }

        public string JobId => _jobInfo.JobId;
        public string OwnerId => _jobInfo.OwnerId;
        public string RepositoryId => _jobInfo.RepositoryId;
        public string DatasetIri => _jobInfo.DatasetIri;
        public string CurrentStatus => _jobInfo.CurrentStatus.ToString();
        public DateTime CompletedAt => _jobInfo.CompletedAt;
        public DateTime StartedAt => _jobInfo.StartedAt;
        public DateTime QueuedAt => _jobInfo.QueuedAt;
        public string JobType => _jobInfo.JobType.ToString();
        public string LogId => _jobInfo.LogId;
    }
}
