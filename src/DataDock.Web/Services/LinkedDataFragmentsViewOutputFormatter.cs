using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using VDS.RDF;

namespace DataDock.Web.Services
{
    public class LinkedDataFragmentsViewOutputFormatter : TextOutputFormatter
    {
        public LinkedDataFragmentsViewOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/html"));
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanWriteType(Type type)
        {
            return typeof(LinkedDataFragmentsViewModel).IsAssignableFrom(type);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var serviceProvider = context.HttpContext.RequestServices;
            var razorViewEngine = serviceProvider.GetService(typeof(IRazorViewEngine)) as IRazorViewEngine;
            var tempDataProvider = serviceProvider.GetService(typeof(ITempDataProvider)) as ITempDataProvider;
            var actionContext = new ActionContext(context.HttpContext, context.HttpContext.GetRouteData(), new ActionDescriptor());
            var viewResult = razorViewEngine.FindView(actionContext, "LinkedDataFragments", false);
            if (!viewResult.Success)
            {
                throw new FileNotFoundException("Unable to locate view for rendering LDF UI");
            }

            var viewModel = context.Object as LinkedDataFragmentsViewModel;
            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()){Model = viewModel};
            using (var sw = new StringWriter())
            {
                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDictionary,
                    new TempDataDictionary(actionContext.HttpContext, tempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);

                await context.HttpContext.Response.WriteAsync(sw.ToString());
            }
        }
    }
}
