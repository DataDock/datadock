using System;
using System.Threading;
using DataDock.Common.Elasticsearch;
using DataDock.Common.Stores;
using DataDock.Common;
using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Nest.JsonNetSerializer;
using NetworkedPlanet.Quince.Git;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace DataDock.Worker
{
    public class Startup
    {
        public virtual void ConfigureServices(IServiceCollection serviceCollection, WorkerConfiguration config)
        {
            var client = RegisterElasticClient(serviceCollection, config);
            ConfigureLogging(config.ElasticsearchUrl);
            ConfigureServices(serviceCollection, client, config);
        }

        protected IElasticClient RegisterElasticClient(IServiceCollection serviceCollection, ApplicationConfiguration config)
        {
            Log.Information("Attempting to connect to Elasticsearch at {esUrl}", config.ElasticsearchUrl);
            var client = new ElasticClient(
                new ConnectionSettings(
                    new SingleNodeConnectionPool(new Uri(config.ElasticsearchUrl)),
                    JsonNetSerializer.Default));
            WaitForElasticsearch(client);
            serviceCollection.AddSingleton<IElasticClient>(client);
            return client;
        }

        protected void WaitForElasticsearch(IElasticClient client)
        {
            Log.Information("Waiting for ES to respond to pings");
            var elasticsearchConnected = false;
            while (!elasticsearchConnected)
            {
                var response = client.Ping();
                if (response.IsValid)
                {
                    Log.Information("Elasticsearch is Running!");
                    elasticsearchConnected = true;
                }
                else
                {
                    Log.Information("Elasticsearch is starting");
                }

                Thread.Sleep(1000);
            }

        }

        protected void ConfigureServices(IServiceCollection serviceCollection, IElasticClient elasticClient,
            WorkerConfiguration config)
        {
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
            serviceCollection.AddSingleton<IProgressLogFactory, SignalrProgressLogFactory>();
            serviceCollection.AddSingleton<ILogStore, DirectoryLogStore>();
            serviceCollection.AddSingleton<IGitHubClientFactory>(new GitHubClientFactory(config.GitHubClientHeader));
            serviceCollection.AddSingleton<IGitWrapperFactory>(new DefaultGitWrapperFactory(config.GitPath));
            serviceCollection.AddSingleton<IQuinceStoreFactory, DefaultQuinceStoreFactory>();
            serviceCollection.AddTransient<IFileGeneratorFactory, FileGeneratorFactory>();
            serviceCollection.AddTransient<IDataDockRepositoryFactory, DataDockRepositoryFactory>();
            serviceCollection.AddSingleton<IGitCommandProcessorFactory, GitCommandProcessorFactory>();
        }

        protected void ConfigureLogging(string elasticsearchUrl)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(new Uri(elasticsearchUrl))
                    {
                        MinimumLogEventLevel = LogEventLevel.Debug,
                        AutoRegisterTemplate = true,
                        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6
                    })
                .CreateLogger();
        }
    }
}
