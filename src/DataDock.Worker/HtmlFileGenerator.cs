using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DataDock.Common;
using NetworkedPlanet.Quince;
using VDS.RDF;

namespace DataDock.Worker
{
    public class HtmlFileGenerator : IResourceStatementHandler
    {
        private readonly IResourceFileMapper _resourceMap;
        private readonly IViewEngine _viewEngine;
        private readonly IProgressLog _progressLog;
        private int _numFilesGenerated;
        private readonly IDataDockUriService _uriService;
        private readonly int _reportInterval;
        private readonly Dictionary<string, object> _addVariables;

        public HtmlFileGenerator(IDataDockUriService uriService, IResourceFileMapper resourceMap, IViewEngine viewEngine, IProgressLog progressLog, int reportInterval, Dictionary<string, object> addVariables)
        {
            _resourceMap = resourceMap;
            _viewEngine = viewEngine;
            _progressLog = progressLog;
            _numFilesGenerated = 0;
            _uriService = uriService;
            _reportInterval = reportInterval;
            _addVariables = addVariables ?? new Dictionary<string, object>();
        }

        private void UpdateVariables(string nquads, string parentDataset)
        {
            // nquads
            if (_addVariables.ContainsKey("nquads"))
            {
                _addVariables.Remove("nquads");
            }
            _addVariables.Add("nquads", nquads);
            // parentDataset
            if (_addVariables.ContainsKey("parentDataset"))
            {
                _addVariables.Remove("parentDataset");
            }
            _addVariables.Add("parentDataset", parentDataset);
            // baseUri
            if (_addVariables.ContainsKey("baseUri"))
            {
                _addVariables.Remove("baseUri");
            }
            _addVariables.Add("baseUri", _uriService.GetBaseUri());
        }


        public bool HandleResource(INode resourceNode, IList<Triple> subjectStatements, IList<Triple> objectStatements)
        {
            if (subjectStatements == null || subjectStatements.Count == 0) return true;
            var subject = (resourceNode as IUriNode)?.Uri;
            var nquads = subject == null ? null : _uriService.GetSubjectDataUrl(subject.ToString(), "nq");
            try
            {
                var targetPath = _resourceMap.GetPathFor(subject);
                if (targetPath != null)
                {
                    targetPath += ".html";
                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    var parentDataset = string.Format("{0}://{1}", subject.Scheme, subject.Authority);
                    if (subject.ToString().IndexOf("id/resource", StringComparison.InvariantCultureIgnoreCase) > 0)
                    {
                        var datasetSegments = subject.Segments.Take(subject.Segments.Length - 1).ToArray();
                        foreach (var segment in datasetSegments)
                        {
                            if (segment.Equals("resource/"))
                            {
                                parentDataset += "dataset/";
                            }
                            else
                            {
                                parentDataset += segment;
                            }
                        }
                        parentDataset = parentDataset.Trim("/".ToCharArray());
                    }
                    UpdateVariables(nquads, parentDataset);

                    var html = _viewEngine.Render(subject, subjectStatements, objectStatements, _addVariables);
                    using (var stream = File.Open(targetPath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        using (var writer = new StreamWriter(stream, Encoding.UTF8))
                        {
                            writer.Write(html);
                        }
                        stream.Close();
                    }
                    _numFilesGenerated++;
                    if (_numFilesGenerated % _reportInterval == 0)
                    {
                        _progressLog.Info("Generating static HTML files - {0} files created/updated.", _numFilesGenerated);
                    }
                }
                else
                {
                    _progressLog.Warn("No target path for {0}, skipping static HTML file generation.", subject);
                }
                
            }
            catch (Exception ex)
            {
                _progressLog.Exception(ex, "Error generating HTML file for subject {0}: {1}", subject, ex.Message);
            }
            return true;
        }
    }
}
