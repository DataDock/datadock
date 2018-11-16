using System;
using System.Threading.Tasks;
using DataDock.Common.Stores;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DataDock.Web.Api
{
    [Produces("application/json")]
    [Route("api/logs")]
    public class LogsController : Controller
    {
        private readonly ILogStore _logStore;
        public LogsController(ILogStore logStore)
        {
            _logStore = logStore;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string logId)
        {
            try
            {
                var logContent = await _logStore.GetLogContentAsync(logId);
                return new ObjectResult(logContent);
            }
            catch (LogNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                Log.ForContext<LogsController>().Error(ex, "Error retrieving log {LogId}", logId);
                return StatusCode(500);
            }
        }
    }
}