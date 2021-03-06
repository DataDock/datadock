﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DataDock.Web.Api
{
    [Produces("application/json")]
    [Route("api/search/{ownerId}/{repoId}")]
    public class RepositoryTagSearchController:Controller
    {
        private readonly IDatasetStore _datasetStore;

        public RepositoryTagSearchController(IDatasetStore datasetStore)
        {
            _datasetStore = datasetStore;
        }

        [HttpGet]
        public async Task<IActionResult> SearchByOwner(string ownerId, string repoId, [FromQuery]string[] tag, bool all = false, bool hidden = false, int skip = 0, int take = 25)
        {
            try
            {
                if (string.IsNullOrEmpty(ownerId) || string.IsNullOrEmpty(repoId))
                    return BadRequest("Invalid owner ID or repository ID");
                if (tag == null || tag.Length == 0 || tag.All(string.IsNullOrEmpty))
                    return BadRequest("At least one tag must be specified");

                var datasets = await _datasetStore.GetDatasetsForTagsAsync(ownerId, repoId, tag, skip, take, all, hidden);
                return Ok(datasets);
            }
            catch (DatasetNotFoundException)
            {
                return Ok(new DatasetInfo[0]);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in SearchByOwnerAndRepository");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
