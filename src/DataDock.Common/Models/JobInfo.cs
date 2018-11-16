using System;
using Nest;

namespace DataDock.Common.Models
{
    [ElasticsearchType(Name="job", IdProperty = "JobId")]
    public class JobInfo
    {
        [Obsolete("Provided for NEST deserialiation only")]
        public JobInfo()
        {

        }

        protected JobInfo(JobRequestInfo req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrEmpty(req.UserId))
                throw new ArgumentException("UserId field must be a non-null, non-empty string", nameof(req));
            if (string.IsNullOrEmpty(req.OwnerId))
                throw new ArgumentException("OwnerId field must be a non-null, non-empty string", nameof(req));
            if (string.IsNullOrEmpty(req.RepositoryId))
                throw new ArgumentException("RepositoryId field must be a non-null, non-empty string", nameof(req));

            JobId = Guid.NewGuid().ToString("N");
            UserId = req.UserId;
            OwnerId = req.OwnerId;
            RepositoryId = req.RepositoryId;
            JobType = req.JobType;
            QueuedAt = DateTime.UtcNow;
            CurrentStatus = JobStatus.Queued;
        }

        public JobInfo(ImportJobRequestInfo req) : this(req as JobRequestInfo)
        {
            DatasetId = req.DatasetId;
            DatasetIri = req.DatasetIri;
            CsvFileName = req.CsvFileName;
            CsvFileId = req.CsvFileId;
            CsvmFileId = req.CsvmFileId;
            IsPublic = req.IsPublic;
            OverwriteExistingData = req.OverwriteExistingData;
        }

        public JobInfo(DeleteJobRequestInfo req) : this(req as JobRequestInfo)
        {
            DatasetId = req.DatasetId;
            DatasetIri = req.DatasetIri;
        }

        public JobInfo(SchemaImportJobRequestInfo req) : this(req as JobRequestInfo)
        {
            SchemaFileId = req.SchemaFileId;
        }

        public JobInfo(SchemaDeleteJobRequestInfo req) : this(req as JobRequestInfo)
        {
            SchemaId = req.SchemaId;
        }

        /// <summary>
        /// The unique identifier for this job
        /// </summary>
        [Keyword]
        public string JobId { get; set; }

        /// <summary>
        /// The identifier for the user who started the job
        /// </summary>
        [Keyword]
        public string UserId { get; set; }

        /// <summary>
        /// The identifier of the owner of the repository that the job will operate on
        /// </summary>
        [Keyword]
        public string OwnerId { get; set; }

        /// <summary>
        /// The repository that this job will operate on
        /// </summary>
        [Keyword]
        public string RepositoryId { get; set; }

        /// <summary>
        /// The type of job
        /// </summary>
        [Number(NumberType.Integer)]
        public JobType JobType { get; set; }

        /// <summary>
        /// The current status of this job
        /// </summary>
        [Number(NumberType.Integer)]
        public JobStatus CurrentStatus { get; set; }

        /// <summary>
        /// The timestamp for the date/time when this job was queued.
        /// </summary>
        /// <remarks>This field is indexed and so can be used for queries. It is set by setting the <see cref="QueuedAt"/> property</remarks>
        [Number(NumberType.Long)]
        public long QueuedTimestamp { get; private set; }

        private DateTime _queuedAt;
        /// <summary>
        /// The date/time when the job was queued.
        /// </summary>
        /// <remarks>This field is not indexed. It is just stored for passing through to the UI.</remarks>
        [Date(Index = false, Store = true)]
        public DateTime QueuedAt
        {
            get => _queuedAt;
            set
            {
                _queuedAt = value;
                QueuedTimestamp = value.Ticks;
            }
        }

        /// <summary>
        /// The date/time when a worker started working on the job.
        /// </summary>
        /// <remarks>This field is not indexed. It is just stored for passing through to the UI</remarks>
        [Date(Index = false, Store = true)]
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// The date/time when the job was completed
        /// </summary>
        /// <remarks>This field is not indexed. It is just stored for passing through to the UI</remarks>
        [Date(Index = false, Store = true)]
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// The timestamp of the date/time when the lock on this job was last refreshed
        /// </summary>
        [Number(NumberType.Long)]
        public long RefreshedTimestamp { get; set; }

        /// <summary>
        /// The identifier assigned to the dataset being imported or deleted
        /// </summary>
        /// <remarks>Required for import and delete jobs</remarks>
        [Keyword(Index = false, Store = true)]
        public string DatasetId { get; set; }

        /// <summary>
        /// The IRI assigned to the dataset being imported
        /// </summary>
        /// <remarks>Required for import and delete jobs</remarks>
        [Keyword(Index = false, Store = true)]
        public string DatasetIri { get; set; }

        /// <summary>
        /// The name of the CSV file being imported
        /// </summary>
        /// <remarks>Required for import jobs</remarks>
        [Keyword(Index = false, Store = true)]
        public string CsvFileName { get; set; }

        /// <summary>
        /// The handle to use to retrieve the CSV file from the DataDock temporary file store
        /// </summary>
        /// <remarks>Required for import jobs</remarks>
        [Keyword(Index = false, Store = true)]
        public string CsvFileId { get; set; }

        /// <summary>
        /// The handle to use to retrieve the CSV metadata file from the DataDock temporary file store
        /// </summary>
        /// <remarks>Required for import jobs</remarks>
        [Keyword(Index = false, Store = true)]
        public string CsvmFileId { get; set; }

        /// <summary>
        /// Flag indicating if the imported dataset should be displayed on public pages
        /// </summary>
        /// <remarks>Required for import jobs. Was originally "ShowOnHomepage", but this flag also controls
        /// whether or not a dataset appears in public search results.</remarks>
        [Boolean(Index = false, Store = true)]
        public bool IsPublic { get; set; }

        /// <summary>
        /// Flag indicating if the imported data overwrites any existing data (if true)
        /// or is added to the existing data (if false)
        /// </summary>
        /// <remarks>Required for import jobs</remarks>
        [Boolean(Index = false, Store = true)]
        public bool OverwriteExistingData { get; set; }

        /// <summary>
        /// The handle to use to retrieve the schema file from the DataDock temporary file store
        /// </summary>
        /// <remarks>Required for schema import jobs</remarks>
        [Keyword(Index = false, Store = true)]
        public string SchemaFileId { get; set; }

        /// <summary>
        /// The identifier assigned to the schema being deleted
        /// </summary>
        /// <remarks>Required for schema delete jobs</remarks>
        [Keyword(Index = false, Store = true)]
        public string SchemaId { get; set; }

        /// <summary>
        /// The identifier of the persistent log file for this job
        /// </summary>
        [Keyword(Index=false, Store=true)]
        public string LogId { get; set; }

        [Ignore]
        public string GitRepositoryUrl => $"https://github.com/{OwnerId}/{RepositoryId}.git";
    }

}
