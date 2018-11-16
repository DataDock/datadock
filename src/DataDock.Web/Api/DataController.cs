using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using DataDock.Common.Stores;
using DataDock.Web.Filters;
using DataDock.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Serilog;

namespace DataDock.Web.Api
{
    [Produces("application/json")]
    [Route("api/data")]
    public class DataController : Controller
    {
        private readonly IUserStore _userStore;
        private readonly IJobStore _jobStore;
        private readonly IImportFormParser _parser;
        private readonly IImportService _importService;

        public DataController(IUserStore userStore, 
            IRepoSettingsStore repoSettingsStore,
            IJobStore jobStore,
            IImportFormParser parser,
            IImportService importService)
        {
            _userStore = userStore;
            _jobStore = jobStore;
            _parser = parser;
            _importService = importService;
        }

        // GET: api/Data
        [HttpGet]
        public HttpResponseMessage Get()
        {
            return new HttpResponseMessage()
            {
                Content = new StringContent("DataDock API")
            };
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Post()
        {
            try
            {
                // Validate user name and authentication status
                var userId = User?.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    Log.Debug("Import: No value found for user principal name");
                    return Unauthorized();
                }

                if (User?.Identity?.IsAuthenticated != true)
                {
                    Log.Debug("Import: User identity is not authenticated");
                    return Unauthorized();
                }

                // Retrieve settings
                var userSettings = await _userStore.GetUserSettingsAsync(userId);
                if (userSettings == null)
                {
                    Log.Debug("Import: User settings not found.");
                    return Unauthorized();
                }

                var parserResult = await _parser.ParseImportFormAsync(Request, userId,
                    async (formData, formCollection) =>
                    {
                        var formValueProvider = new FormValueProvider(
                            BindingSource.Form,
                            formCollection,
                            CultureInfo.CurrentCulture);
                        return await TryUpdateModelAsync(formData, "", formValueProvider);
                    });
                if (!parserResult.IsValid)
                {
                    if (!ModelState.IsValid) return BadRequest(ModelState);
                    return BadRequest(parserResult.ValidationErrors);
                }

                var jobRequest = parserResult.ImportJobRequest;
                try
                {
                    var repoSettings =
                        await _importService.CheckRepoSettingsAsync(User, jobRequest.OwnerId, jobRequest.RepositoryId);
                }
                catch (Exception e)
                {
                    Log.Error(
                        $"api/data: unable to retrive repoSettings for the supplied owner '{jobRequest.OwnerId}' and repo '{jobRequest.RepositoryId}'");
                    return BadRequest(
                        "Repository does not exist or you do not have the required authorization to publish to it.");
                }

                var job = await _jobStore.SubmitImportJobAsync(jobRequest);
                
                Log.Information("api/data(POST): Conversion job started.");

                var queuedJobIds = new List<string> {job.JobId};

                if (parserResult.SchemaImportJobRequest != null)
                {
                    try
                    {
                        var schemaJob = await _jobStore.SubmitSchemaImportJobAsync(parserResult.SchemaImportJobRequest);

                        Log.Information("api/data(POST): Schema creation job started.");
                        queuedJobIds.Add(schemaJob.JobId);
                    }
                    catch (Exception)
                    {
                        Log.Error("api/data(POST): Unexpected error staring schema creation job.");
                    }
                }

                return Ok(new DataControllerResult { StatusCode = 200, Message = "API called successfully", Metadata = parserResult.Metadata, JobIds = queuedJobIds });
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Fatal error in api/data '{ex.Message}'");
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }
    }

    /// <summary>
    /// Structure returned from the Post method of the DataController
    /// </summary>
    public class DataControllerResult
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string Metadata { get; set; }
        public List<string> JobIds { get; set; }
    }

}