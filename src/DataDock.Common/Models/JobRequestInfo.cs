namespace DataDock.Common.Models
{
    public abstract class JobRequestInfo
    {
        /// <summary>
        /// The identifier for the user who started the job
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The identifier of the owner of the repository that the job will operate on
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// The repository that this job will operate on
        /// </summary>
        public string RepositoryId { get; set; }

        public abstract JobType JobType { get; }
    }

    public class ImportJobRequestInfo : JobRequestInfo
    {
        /// <summary>
        /// The identifier of the dataset to be created/updated
        /// </summary>
        public string DatasetId { get; set; }

        /// <summary>
        /// The IRI of the dataset graph to be created/updated
        /// </summary>
        public string DatasetIri { get; set; }

        /// <summary>
        /// The name of the CSV file to be imported
        /// </summary>
        public string CsvFileName { get; set; }

        /// <summary>
        /// The handle to use to retrieve the CSV file content from the DataDock file repository
        /// </summary>
        public string CsvFileId { get; set; }

        /// <summary>
        /// The handle to use to retrieve the CSV metadata file content from the DataDock file repository
        /// </summary>
        public string CsvmFileId { get; set; }

        /// <summary>
        /// Flag indicating if the imported dataset should appear on public pages
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Flag indicating if the imported data should overwrite or add to any existing data in the dataset
        /// </summary>
        public bool OverwriteExistingData { get; set; }

        public override JobType JobType => JobType.Import;
    }

    public class DeleteJobRequestInfo : JobRequestInfo
    {
        /// <summary>
        /// The identifier of the dataset to be deleted
        /// </summary>
        public string DatasetId { get; set; }

        /// <summary>
        /// The IRI of the dataset graph to be deleted
        /// </summary>
        public string DatasetIri { get; set; }
        public override JobType JobType => JobType.Delete;
    }

    public class SchemaImportJobRequestInfo : JobRequestInfo
    {
        /// <summary>
        /// The handle to use to retrieve the schema file content from the DataDock file repository
        /// </summary>
        public string SchemaFileId { get; set; }
        public override JobType JobType => JobType.SchemaCreate;
    }

    public class SchemaDeleteJobRequestInfo : JobRequestInfo
    {
        /// <summary>
        /// The identifier of the schema to be deleted
        /// </summary>
        public string SchemaId { get; set; }
        public override JobType JobType => JobType.SchemaDelete;
    }
}