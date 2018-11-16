using DataDock.Common.Models;
using Serilog;
using System;
using System.Collections.Generic;

namespace DataDock.Web.ViewModels
{
    public class TemplateDeleteModel : DashboardViewModel
    {
        public string SchemaId { get; set; }

        public SchemaInfo SchemaInfo { get; set; }

        public TemplateDeleteModel()
        {
        }

        public TemplateDeleteModel(string schemaId)
        {
            SchemaId = schemaId;
        }

        public string GetSchemaTitle()
        {
            try
            {
                if (SchemaInfo != null)
                {
                    var viewModel = new TemplateViewModel(SchemaInfo);
                    return viewModel.Title;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error retrieving title from schema '{SchemaInfo?.Id}'");
            }
            return string.Empty;
        }
        

        public bool HasErrored { get; set; }
        public List<string> Errors { get; set; }
    }
}
