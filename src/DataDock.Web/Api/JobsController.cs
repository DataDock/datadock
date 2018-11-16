using System;
using System.Collections.Generic;
using DataDock.Common.Stores;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using DataDock.Common.Models;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace DataDock.Web.Api
{
    [Produces("application/json")]
    [Route("api/jobs")]
    public class JobsController : Controller
    {
        private readonly IJobStore _jobStore;

        public JobsController(IJobStore jobStore)
        {
            _jobStore = jobStore;
        }

        // GET: api/Data
        [HttpGet]
        public async Task<IActionResult> Get(string ownerId, string repoId = "")
        {
            try
            {
                if (string.IsNullOrEmpty(ownerId)) return BadRequest("Missing argument: ownerId");

                try
                {
                    // get jobs
                    if (string.IsNullOrEmpty(repoId))
                    {
                        //by owner
                        var ownerJobs = await _jobStore.GetJobsForOwner(ownerId);
                        return new ObjectResult(ownerJobs);
                    }

                    var repoJobs = await _jobStore.GetJobsForRepository(ownerId, repoId);
                    return new ObjectResult(repoJobs);
                }
                catch (JobNotFoundException jnf)
                {
                    var empty = new List<JobInfo>();
                    return new ObjectResult(empty);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error retrieving jobs for owner '{ownerId}' and repo '{repoId}' (optional)", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }


    }
}
