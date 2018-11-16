namespace DataDock.Web.ViewModels
{
    public class DashboardViewModel : BaseLayoutViewModel
    {
        public string SelectedOwnerId { get; set; }
        public string SelectedRepoId { get; set; }

        public string UserId { get; set; }

        private string _area;
        public string Area
        {
            get => _area;
            set
            {
                _area = value;
                if (string.IsNullOrEmpty(value))
                {
                    this.Title = "DataDock";
                    this.Heading = SelectedOwnerId ?? "";
                }
                else
                {
                    var dashContext = SelectedOwnerId;
                    if (!string.IsNullOrEmpty(SelectedRepoId))
                    {
                        dashContext = dashContext + "/" + SelectedRepoId;
                    }
                    switch (value)
                    {
                        case "summary":
                            this.Heading = "Dashboard";
                            
                            break;
                        case "repositories":
                            this.Heading = "Repositories";
                            break;
                        case "datasets":
                            this.Heading = "Datasets";
                            break;
                        case "library":
                            this.Heading = "Template Library";
                            break;
                        case "import":
                            this.Heading = "Add Data";
                            break;
                        case "jobs":
                            this.Heading = "Job History";
                            break;
                        case "settings":
                            this.Heading = "Settings";
                            break;
                        default:
                            this.Heading = "";
                            break;
                    }
                    this.Title = string.Format("DataDock.io > {0}{1}", dashContext, !string.IsNullOrEmpty(this.Heading) ? ": " + this.Heading : "");
                }
            }
        }

        /// <summary>
        /// Optional SchemaId for when a template has been selected for use on a new import
        /// </summary>
        public string SelectedSchemaId { get; set; }

        /// <summary>
        /// Optional DatasetId for when a dataset has been selected for view/admin
        /// </summary>
        public string SelectedDatasetId { get; set; }
    }
}
