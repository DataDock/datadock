using DataDock.Common.Stores;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DataDock.Web.ViewModels;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "JobLog")]
    public class JobLogViewComponent : ViewComponent
    {
        private readonly IJobStore _jobStore;
        private readonly ILogStore _logStore;
        public JobLogViewComponent(IJobStore jobStore, ILogStore logStore)
        {
            _jobStore = jobStore;
            _logStore = logStore;
        }

        public async Task<IViewComponentResult> InvokeAsync(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId)) return View("Empty");

                // get log id from job
                var job = await _jobStore.GetJobInfoAsync(jobId);
                if (job == null || string.IsNullOrEmpty(job.LogId))
                {
                    return View("Empty");
                }

                var log = await _logStore.GetLogContentAsync(job.LogId);
                if (log == null)
                {
                    return View("Empty");
                }
                var jhvm = new JobHistoryViewModel(job);
                ViewData["LogContents"] = log.Trim();
                return View("Default", jhvm);
            }
            catch (Exception e)
            {
                return View("Error", e);
            }   
        }
    }
}
