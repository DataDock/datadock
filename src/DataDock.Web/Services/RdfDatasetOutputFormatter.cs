using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (typeof(ITripleStore).IsAssignableFrom(type)) return true;
            return false;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            await Task.Run(() =>
            {
                var mediaType = context.ContentType;
                var definition = MimeTypesHelper.GetDefinitions("application/n-quads").FirstOrDefault();
                var writer = definition.GetRdfDatasetWriter();
                var response = context.HttpContext.Response;
                var store = context.Object as ITripleStore;
                using (var responseWriter = new StreamWriter(response.Body, selectedEncoding, 1024, true))
                {
                    writer.Save(store, responseWriter, true);
                }
            });
        }
    }
}
