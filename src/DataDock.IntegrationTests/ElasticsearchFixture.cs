using System;
using System.IO;
using DataDock.Common;
using DataDock.Worker;
using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;

namespace DataDock.IntegrationTests
{
    public class ElasticsearchFixture : IDisposable
    {
        public ApplicationConfiguration Configuration { get; }
        public WorkerConfiguration WorkerConfiguration { get; }

        public ElasticClient Client { get; }

        public ElasticsearchFixture()
        {
            var esUrl = Environment.GetEnvironmentVariable("ELASTICSEARCH_URL") ?? "http://localhost:9200";
            var pool = new SingleNodeConnectionPool(new Uri(esUrl));
            var connectionSettings = new ConnectionSettings(pool, sourceSerializer: JsonNetSerializer.Default);
            Client = new ElasticClient(connectionSettings);
            var indexSuffix = "_" + DateTime.UtcNow.Ticks;
            Configuration = new ApplicationConfiguration()
            {
                ElasticsearchUrl = esUrl,
                JobsIndexName = "test_jobs" + indexSuffix,
                UserIndexName = "test_usersettings" + indexSuffix,
                OwnerSettingsIndexName = "test_ownersettings" + indexSuffix,
                RepoSettingsIndexName = "test_reposettings" + indexSuffix,
                DatasetIndexName = "test_datasets" + indexSuffix,
                SchemaIndexName = "test_schemas" + indexSuffix,
                FileStorePath = "test_files"  + indexSuffix
            };
            WorkerConfiguration = new WorkerConfiguration{
                ElasticsearchUrl = esUrl,
                JobsIndexName = "test_jobs" + indexSuffix,
                UserIndexName = "test_usersettings" + indexSuffix,
                OwnerSettingsIndexName = "test_ownersettings" + indexSuffix,
                RepoSettingsIndexName = "test_reposettings" + indexSuffix,
                DatasetIndexName = "test_datasets" + indexSuffix,
                SchemaIndexName = "test_schemas" + indexSuffix,
                FileStorePath = "test_files" + indexSuffix,
                GitPath = "",
                RepoBaseDir = "test_repos" + indexSuffix,
                GitHubClientHeader = "datadock_test"
            };
        }

        public void Dispose()
        {
            if (Client.IndexExists(Configuration.DatasetIndexName).Exists) Client.DeleteIndex(Configuration.DatasetIndexName);
            if (Client.IndexExists(Configuration.JobsIndexName).Exists) Client.DeleteIndex(Configuration.JobsIndexName);
            if (Client.IndexExists(Configuration.OwnerSettingsIndexName).Exists)
                Client.DeleteIndex(Configuration.OwnerSettingsIndexName);
            if (Client.IndexExists(Configuration.RepoSettingsIndexName).Exists) Client.DeleteIndex(Configuration.RepoSettingsIndexName);
            if (Client.IndexExists(Configuration.SchemaIndexName).Exists) Client.DeleteIndex(Configuration.SchemaIndexName);
            if (Client.IndexExists(Configuration.UserIndexName).Exists) Client.DeleteIndex(Configuration.UserIndexName);
            if (Directory.Exists(Configuration.FileStorePath)) Directory.Delete(Configuration.FileStorePath, true);
        }
    }
}
