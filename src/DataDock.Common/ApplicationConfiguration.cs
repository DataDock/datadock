using System;
using Serilog;

namespace DataDock.Common
{
    public class ApplicationConfiguration
    {
        /// <summary>
        /// The root URL for published datasets
        /// </summary>
        public string PublishUrl { get; set; } = "http://datadock.io/";

        public string GitHubClientHeader { get; set; } = "";

        public string ElasticsearchUrl { get; set; } = "http://elasticsearch:9200";

        public string JobsIndexName { get; set; } = "jobs";

        public string UserIndexName { get; set; } = "users";

        public string OwnerSettingsIndexName { get; set; } = "ownersettings";

        public string RepoSettingsIndexName { get; set; } = "reposettings";

        public string DatasetIndexName { get; set; } = "datasets";

        public string SchemaIndexName { get; set; } = "schemas";

        public string FileStorePath { get; set; } = "/datadock/repositories";

        public string LogStorePath { get; set; } = "/datadock/logs";

        public int LogTimeToLive { get; set; } = 90;

        public virtual void LogSettings()
        {
            Log.Information("Configured Elasticsearch Url {ElasticsearchUrl}", ElasticsearchUrl);
            Log.Information("Configured Jobs Index {JobsIndexName}", JobsIndexName);
            Log.Information("Configured User Index {UserIndexName}", UserIndexName);
            Log.Information("Configured Owner Settings Index {OwnerSettingsIndexName}", OwnerSettingsIndexName);
            Log.Information("Configured Repository Settings Index {RepoSettingsIndexName}", RepoSettingsIndexName);
            Log.Information("Configured DatasetIndex {DatasetIndexName}", DatasetIndexName);
            Log.Information("Configured Schema Index {SchemaIndexName}", SchemaIndexName);
            Log.Information("Configured File Store Path {FileStorePath}", FileStorePath);
        }
    }
}