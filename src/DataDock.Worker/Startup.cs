using System;
using DataDock.Common.Elasticsearch;
using DataDock.Common.Stores;
using DataDock.Common;
using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Nest.JsonNetSerializer;
using NetworkedPlanet.Quince.Git;
using Serilog;

namespace DataDock.Worker
{
    public class Startup
    {
        public virtual void ConfigureServices(IServiceCollection serviceCollection, WorkerConfiguration config)
        {
            RegisterElasticClient(serviceCollection, config);
            serviceCollection.AddSingleton(config);
            serviceCollection.AddSingleton<ApplicationConfiguration>(config);
            serviceCollection.AddScoped<IFileStore, DirectoryFileStore>();

            serviceCollection.AddSingleton<IDataDockUriService>(new DataDockUriService(config.PublishUrl));
            serviceCollection.AddSingleton<IDatasetStore, DatasetStore>();
            serviceCollection.AddSingleton<IJobStore, JobStore>();
            serviceCollection.AddSingleton<IUserStore, UserStore>();
            serviceCollection.AddSingleton<IOwnerSettingsStore, OwnerSettingsStore>();
            serviceCollection.AddSingleton<IRepoSettingsStore, RepoSettingsStore>();
            serviceCollection.AddSingleton<ISchemaStore, SchemaStore>();
            serviceCollection.AddSingleton<IProgressLogFactory, SignalRProgressLogFactory>();
            serviceCollection.AddSingleton<ILogStore, DirectoryLogStore>();
            serviceCollection.AddSingleton<IGitHubClientFactory>(new GitHubClientFactory(config.GitHubClientHeader));
            serviceCollection.AddSingleton<IGitWrapperFactory>(new DefaultGitWrapperFactory(config.GitPath));
            serviceCollection.AddSingleton<IQuinceStoreFactory, DefaultQuinceStoreFactory>();
            serviceCollection.AddTransient<IFileGeneratorFactory, FileGeneratorFactory>();
            serviceCollection.AddTransient<IDataDockRepositoryFactory, DataDockRepositoryFactory>();
            serviceCollection.AddSingleton<IGitCommandProcessorFactory, GitCommandProcessorFactory>();
        }

        protected void RegisterElasticClient(IServiceCollection serviceCollection, ApplicationConfiguration config)
        {
            Log.Information("Attempting to connect to Elasticsearch at {esUrl}", config.ElasticsearchUrl);
            var client = new ElasticClient(
                new ConnectionSettings(
                    new SingleNodeConnectionPool(new Uri(config.ElasticsearchUrl)),
                    JsonNetSerializer.Default));
            WaitForElasticsearch(client);
            serviceCollection.AddSingleton<IElasticClient>(client);
        }

        protected void WaitForElasticsearch(IElasticClient client)
        {
            Log.Information("Waiting for Elasticsearch cluster");
            client.WaitForInitialization();
            Log.Information("Elasticsearch cluster is now available");
        }
    }
}
