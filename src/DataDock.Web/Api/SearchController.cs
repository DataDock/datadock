using DataDock.Common.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataDock.Web.Api
{
    [Produces("application/json")]
    [Route("api/search")]
    public class SearchController : Controller
    {
        private readonly IDatasetStore _datasetStore;

        public SearchController(IDatasetStore datasetStore)
        {
            _datasetStore = datasetStore;
        }
        [Route("api/search/{owner}")]
        [HttpGet]
        public async Task<IActionResult> SearchByOwner(string owner, [FromQuery]string[] tag, bool all = false, bool hidden = false, int skip = 0, int take = 25)
        {
            try
            {
                if (string.IsNullOrEmpty(owner))
                    return BadRequest("Invalid owner ID");
                if (tag == null || tag.Length == 0 || tag.All(string.IsNullOrEmpty))
                    return BadRequest("At least one tag must be specified");
                
                var datasets = await _datasetStore.GetDatasetsForTagsAsync(owner, tag, skip, take, all, hidden);
                return Ok(datasets);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in SearchByOwner");
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }

        [Route("api/search/{owner}/{repository}")]
        [HttpGet]
        public async Task<IActionResult> SearchByOwnerAndRepository(string owner, string repository, [FromQuery]string[] tag, bool all = false, bool hidden = false, int skip = 0, int take = 25)
        {
            try
            {
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repository))
                    return BadRequest("Invalid owner or repository ID");
                if (tag == null || tag.Length == 0 || tag.All(string.IsNullOrEmpty))
                    return BadRequest("At least one tag must be specified");
                
                var datasets = await  _datasetStore.GetDatasetsForTagsAsync(owner, repository, tag, skip, take, all, hidden);
                return Ok(datasets);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in SearchByOwnerAndRepository");
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }
    }
}
