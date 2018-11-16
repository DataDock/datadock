using DataDock.Common.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Threading.Tasks;

namespace DataDock.Web.Api
{
    [Produces("application/json")]
    [Route("api/schemas")]
    public class SchemasController : Controller
    {
        private readonly ISchemaStore _schemaStore;

        public SchemasController(ISchemaStore schemaStore)
        {
            _schemaStore = schemaStore;
        }

        // GET: api/Data
        [HttpGet]
        public async Task<IActionResult> Get(string ownerId, string schemaId)
        {
            if (string.IsNullOrEmpty(ownerId)) return BadRequest("Missing argument: ownerId");
            if (string.IsNullOrEmpty(schemaId)) return BadRequest("Missing argument: schemaId");

            try
            {
                try
                {
                    var schema = await _schemaStore.GetSchemaInfoAsync(ownerId, schemaId);
                    return new ObjectResult(schema);
                }
                catch (DatasetNotFoundException dnf)
                {
                    return new ObjectResult(null);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error retrieving schema for owner '{ownerId}' with ID '{schemaId}'", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }


    }
}
