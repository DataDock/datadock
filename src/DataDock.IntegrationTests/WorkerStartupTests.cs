using System;
using DataDock.Common;
using DataDock.Common.Elasticsearch;
using DataDock.Common.Stores;
using DataDock.Worker;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class WorkerServiceCollectionTests : IClassFixture<ElasticsearchFixture>
    {
        private readonly IServiceProvider _services;

        public WorkerServiceCollectionTests(ElasticsearchFixture esFixture)
        {
            var serviceCollection = new ServiceCollection();
            var startup = new Startup();
            startup.ConfigureServices(serviceCollection, esFixture.WorkerConfiguration);
            _services = serviceCollection.BuildServiceProvider();
        }

        [Theory]
        [InlineData(typeof(IDatasetStore), typeof(DatasetStore))]
        [InlineData(typeof(IFileStore), typeof(DirectoryFileStore))]
        [InlineData(typeof(IOwnerSettingsStore), typeof(OwnerSettingsStore))]
        [InlineData(typeof(IRepoSettingsStore), typeof(RepoSettingsStore))]
        [InlineData(typeof(IDataDockRepositoryFactory), typeof(DataDockRepositoryFactory))]
        [InlineData(typeof(IGitCommandProcessorFactory), typeof(GitCommandProcessorFactory))]
        [InlineData(typeof(IProgressLogFactory), typeof(SignalrProgressLogFactory))]
        [InlineData(typeof(IDataDockUriService), typeof(DataDockUriService))]
        public void ItProvidesExpectedServices(Type serviceType, Type expectImplType)
        {
            var service = _services.GetService(serviceType);
            service.Should().NotBeNull();
            service.Should().BeOfType(expectImplType);
        }
    }
}
