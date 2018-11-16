using DataDock.Common.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Threading.Tasks;

namespace DataDock.Web.Api
{
    [Produces("application/json")]
    [Route("api/datasets")]
    public class DatasetsController : Controller
    {
        private readonly IDatasetStore _datasetStore;

        public DatasetsController(IDatasetStore datasetStore)
        {
            _datasetStore = datasetStore;
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
                    // get datasets
                    if (string.IsNullOrEmpty(repoId))
                    {
                        //by owner
                        var ownerDatasets = await _datasetStore.GetDatasetsForOwnerAsync(ownerId, 0, 20);
                        return new ObjectResult(ownerDatasets);
                    }

                    var repoDatasets = await _datasetStore.GetDatasetsForRepositoryAsync(ownerId, repoId, 0, 20);
                    return new ObjectResult(repoDatasets);
                }
                catch (DatasetNotFoundException dnf)
                {
                    return new ObjectResult(null);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error retrieving datasets for owner '{ownerId}' and repo '{repoId}' (optional)", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }


    }
}
