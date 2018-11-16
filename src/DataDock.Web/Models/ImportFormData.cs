namespace DataDock.Web.Models
{
    public class ImportFormData
    {
        public string OwnerId { get; set; }
        public string RepoId { get; set; }
        public string Filename { get; set; }
        public bool ShowOnHomePage { get; set; }
        public bool OverwriteExisting { get; set; }
        public bool SaveAsSchema { get; set; }
        public string Metadata { get; set; }
    }
}
