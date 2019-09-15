using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using VDS.RDF;

namespace DataDock.Web.Services
{
    public class RdfDatasetOutputFormatter : TextOutputFormatter
    {
        public RdfDatasetOutputFormatter()
        {
            foreach (var mimeTypeDef in MimeTypesHelper.Definitions)
            {
                if (mimeTypeDef.CanWriteRdfDatasets)
                {
                    foreach (var mimeType in mimeTypeDef.MimeTypes)
                    {
                        if (!SupportedMediaTypes.Contains(mimeType))
                        {
                            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(mimeType));
                        }
                    }
                }
            }
            SupportedEncodings.Add(Encoding.UTF8);
        }

        protected override bool CanWriteType(Type type)
        {
            return typeof(LinkedDataFragmentsViewModel).IsAssignableFrom(type);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            await Task.Run(() =>
            {
                var contentType = MediaTypeHeaderValue.Parse(context.ContentType);
                var definition = MimeTypesHelper.GetDefinitions(contentType.MediaType.Value).FirstOrDefault();
                var writer = definition.GetRdfDatasetWriter();
                var response = context.HttpContext.Response;
                var model = context.Object as LinkedDataFragmentsViewModel;
                var store = new TripleStore();
                
                if (model.ResultsGraph != null)
                {
                    store.Add(model.ResultsGraph);
                }
                store.Add(model.MetadataGraph);
                using (var responseWriter = new StreamWriter(response.Body, selectedEncoding, 1024, true))
                {
                    writer.Save(store, responseWriter, true);
                }
            });
        }
    }
}
