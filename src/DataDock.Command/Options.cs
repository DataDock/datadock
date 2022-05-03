using CommandLine;

namespace DataDock.Command
{
    class SharedOptions
    {
        [Value(0, MetaName = "<REPO_URI>", HelpText = "The base URI for the data repository")]
        public Uri RepositoryUri { get; set; } = null!;

        [Value(1, HelpText = "The short identifier for the dataset being modified.", Required = true)]
        public string DatasetId { get; set; } = null!;

        [Option('r', "repository", HelpText = "The path to the DataDock repository to import into.", Default = ".")]
        public string RepositoryPath { get; set; } = ".";

    }

    [Verb("import", isDefault:true, HelpText = "Add data to a DataDock Repository")]
    class ImportOptions:SharedOptions
    {

        [Value(2, HelpText = "The path to the file containing the data to be imported.")]
        public string File { get; set; } = null!;

        [Value(3, HelpText = "The path to the CSVW metadata file to use when importing a CSV file.")]
        public string MetadataFile { get; set; } = null!;

        [Option('o', "overwrite", HelpText = "Whether to overwrite any existing data in the dataset being updated.", Default = false)]
        public bool Overwrite { get; set; }

        [Option('l', "download-links", HelpText = "Data download links for the updated dataset.")]
        public IEnumerable<Uri> DownloadLinks { get; set; } = Enumerable.Empty<Uri>();

    }

    [Verb("delete", HelpText = "Remove data from a DataDock Repository")]
    class DeleteOptions:SharedOptions
    {
    }

    
}
