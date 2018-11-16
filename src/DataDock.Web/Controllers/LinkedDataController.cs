using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using DataDock.Common.Stores;
using DataDock.Web.Models;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nest;
using Serilog;

namespace DataDock.Web.Controllers
{
    public class LinkedDataController : Controller
    {
        private readonly IOwnerSettingsStore _ownerSettingsStore;
        private readonly IRepoSettingsStore _repoSettingsStore;

        private static readonly string[] SupportedPageMediaTypes = { "text/html", "*/*" };
        private static readonly string[] SupportedDataMediaTypes = { "application/n-quads", "*/*" };
        private static readonly string[] SupportedMediaTypes = {"text/html", "application/n-quads", "*/*"};
        private static readonly string[] SupportedCsvMediaTypes = {"text/csv", "*/*"};

        private static readonly string[] SupportedCsvMetadataMediaTypes =
            {"application/json", "application/csvm+json", "application/ld+json", "*/*"};

        public LinkedDataController(IOwnerSettingsStore ownerSettingsStore, IRepoSettingsStore repoSettingsStore)
        {
            _ownerSettingsStore = ownerSettingsStore;
            _repoSettingsStore = repoSettingsStore;
        }

        public async Task<ActionResult> Owner(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                return RedirectToAction("Index", "Home");
            }
            var ownerSettings = await _ownerSettingsStore.GetOwnerSettingsAsync(ownerId);

            var portalViewModel = new PortalViewModel(ownerSettings);
            

            // repos
            try
            {
                var repos = await _repoSettingsStore.GetRepoSettingsForOwnerAsync(ownerId);
                portalViewModel.RepoIds = repos.Select(r => r.RepositoryId).ToList();
            }
            catch (Exception e)
            {
                Log.Error(e, "Error getting repositories for {0}", ownerId);
            }
            
            return View("Index", portalViewModel); ;
        }

        /// <summary>
        /// Redirects a request for a repository identifier in the form {BASE_URL}/{ownerId}/{repoId} to either the
        /// HTML or the N-Quads VoID representation
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <returns></returns>
        public IActionResult Repository(string ownerId, string repoId)
        {
            var requestMediaType = Request.GetTypedHeaders().Accept.OrderByDescending(x => x.Quality ?? 0.0)
                .FirstOrDefault(x => SupportedMediaTypes.Contains(x.MediaType.Value));
            if (requestMediaType == null) return new StatusCodeResult(StatusCodes.Status406NotAcceptable);
            switch (requestMediaType.MediaType.Value)
            {
                case "application/n-quads":
                    return SeeOther($"/{ownerId}/{repoId}/data/void.nq");
                default:
                    return SeeOther($"/{ownerId}/{repoId}/page/index.html");
            }
        }

        /// <summary>
        /// Redirects a resource identifier to either the HTML page or the RDF data representation
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public IActionResult Identifier(string ownerId, string repoId, string path)
        {
            var requestMediaType = Request.GetTypedHeaders().Accept.OrderByDescending(x => x.Quality ?? 0.0)
                .FirstOrDefault(x => SupportedMediaTypes.Contains(x.MediaType.Value));
            if (requestMediaType == null) return new StatusCodeResult(StatusCodes.Status406NotAcceptable);
            switch (requestMediaType.MediaType.Value)
            {
                case "application/n-quads":
                    return SeeOther($"/{ownerId}/{repoId}/data/{path}.nq");
                default:
                    return SeeOther($"/{ownerId}/{repoId}/page/{path}.html");
            }
        }

        /// <summary>
        /// Proxies a request for an HTML representation of a resource through to the GitHub pages site for the repository
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<IActionResult> Page(string ownerId, string repoId, string path)
        {
            var requestMediaType = Request.GetTypedHeaders().Accept.OrderByDescending(x => x.Quality ?? 0.0)
                .FirstOrDefault(x => SupportedPageMediaTypes.Contains(x.MediaType.Value));
            if (requestMediaType == null) return new StatusCodeResult(StatusCodes.Status406NotAcceptable);
            return await ProxyRequest(new Uri($"https://{ownerId}.github.io/{repoId}/page/{path}"));
        }

        /// <summary>
        /// Proxies a request for an RDF representation of a resource through to the GitHub pages site for the repository
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<IActionResult> Data(string ownerId, string repoId, string path)
        {
            var requestMediaType = SelectMediaType(SupportedDataMediaTypes, SupportedDataMediaTypes[0]);
            if (requestMediaType == null) return new StatusCodeResult(StatusCodes.Status406NotAcceptable);
            switch (requestMediaType)
            {
                case "application/n-quads":
                case "*/*":
                    return await ProxyRequest(
                        new Uri($"https://{ownerId}.github.io/{repoId}/data/{path}"), 
                        requestMediaType);
                default:
                    return new StatusCodeResult(StatusCodes.Status406NotAcceptable);
            }
        }

        public async Task<IActionResult> Csv(string ownerId, string repoId, string datasetId, string filename)
        {
            var requestedMediaType = SelectMediaType(SupportedCsvMediaTypes, SupportedCsvMediaTypes[0]);
            if (requestedMediaType == null) return new StatusCodeResult(StatusCodes.Status406NotAcceptable);
            return await ProxyRequest(
                new Uri($"https://{ownerId}.github.io/{repoId}/csv/{datasetId}/{filename}.csv"),
                requestedMediaType);
        }

        public async Task<IActionResult> CsvMetadata(string ownerId, string repoId, string datasetId, string filename)
        {
            var requestedMediaType = SelectMediaType(SupportedCsvMetadataMediaTypes, SupportedCsvMediaTypes[0]);
            if (requestedMediaType == null) return new StatusCodeResult(StatusCodes.Status406NotAcceptable);
            return await ProxyRequest(
                new Uri($"https://{ownerId}.github.io/{repoId}/csv/{datasetId}/{filename}.json"),
                requestedMediaType);
        }

        private string SelectMediaType(IEnumerable<string> options, string defaultMediaType)
        {
            var mt =  Request.GetTypedHeaders().Accept.OrderByDescending(x => x.Quality ?? 0.0)
                .Select(x => x.MediaType.Value)
                .FirstOrDefault(options.Contains);
            return "*/*".Equals(mt) ? defaultMediaType : mt;
        }

        /// <summary>
        /// Basic proxy functionality
        /// </summary>
        /// <param name="remoteUri"></param>
        /// <param name="overrideContentType">OPTIONAL: The media type to return as the Content-Type header if the proxied server returns application/octet-stream</param>
        /// <returns></returns>
        private async Task<IActionResult> ProxyRequest(Uri remoteUri, string overrideContentType=null)
        {
            using (var http = new HttpClient())
            {
                // TODO: Add proxy headers
                var response =  await http.GetAsync(remoteUri);
                var proxiedContentType = response.Content.Headers.ContentType.ToString();
                this.Response.StatusCode = (int)response.StatusCode;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    this.Response.ContentType =
                        proxiedContentType.Equals("application/octet-stream") && overrideContentType != null
                            ? overrideContentType
                            : proxiedContentType;
                    // TODO: Copy other headers - e.g. cache-control?
                    await response.Content.CopyToAsync(this.Response.Body);
                }

                return new EmptyResult();
            }
        }

        /// <summary>
        /// Convenience wrapper for creating a 303 See Other result
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private IActionResult SeeOther(string url)
        {
            return new SeeOtherResult(url);
        }
    }

    /// <summary>
    /// Represents an HTTP 303 result with the redirect location specified in the response header
    /// </summary>
    public class SeeOtherResult : IActionResult
    {
        private readonly string _location;

        public SeeOtherResult(string location)
        {
            _location = Uri.EscapeUriString(location);
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            await Task.Run(() =>
            {
                context.HttpContext.Response.StatusCode = 303;
                context.HttpContext.Response.Headers["Location"] = _location;
            });
        }
    }
}
