using System;
using System.Collections.Generic;
using DataDock.Common;
using NetworkedPlanet.Quince;

namespace DataDock.Worker
{
    public class FileGeneratorFactory : IFileGeneratorFactory
    {
        public ITripleCollectionHandler MakeRdfFileGenerator(
            IResourceFileMapper resourceMap, 
            IEnumerable<Uri> graphFilter,
            IProgressLog progressLog,
            int reportInterval)
        {
            return new RdfFileGenerator(resourceMap, graphFilter, progressLog, reportInterval);
        }

        public IResourceStatementHandler MakeHtmlFileGenerator(
            IDataDockUriService uriService, 
            IResourceFileMapper resourceMap, 
            IViewEngine viewEngine, 
            IProgressLog progressLog, 
            int reportInterval,
            Dictionary<string, object> addVariables)
        {
            return new HtmlFileGenerator(uriService, resourceMap, viewEngine, progressLog, reportInterval, addVariables);
        }
    }
}