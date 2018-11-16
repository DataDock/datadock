using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class WorkerJsonConfigurationTests
    {
        [Fact]
        public void ItCanUseDefaultValues()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.GetFullPath("data/config"))
                .AddJsonFile("partialSettings.json")
                .AddEnvironmentVariables("DDX_"); // different prefix to avoid any test race conditions with the test that uses DD_ env vars

            var config = builder.Build();
            var appConfig = new WorkerConfiguration();
            config.Bind(appConfig);
            // Expected overrides
            appConfig.ElasticsearchUrl.Should().Be("http://some.elasticsearch:9200/");
            appConfig.FileStorePath.Should().Be("/path/to/file/store");
            // Expected defaults
            appConfig.DatasetIndexName.Should().Be("datasets");
            appConfig.JobsIndexName.Should().Be("jobs");
            appConfig.OwnerSettingsIndexName.Should().Be("ownersettings");
            appConfig.SchemaIndexName.Should().Be("schemas");
            appConfig.RepoSettingsIndexName.Should().Be("reposettings");
            appConfig.UserIndexName.Should().Be("users");
            appConfig.GitPath.Should().Be("git");
            appConfig.GitHubClientHeader.Should().Be("");
            appConfig.PublishUrl.Should().Be("http://datadock.io/");
            appConfig.RepoBaseDir.Should().Be("/datadock/repositories");

        }
        [Fact]
        public void ItCanReadFromJson()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.GetFullPath("data/config"))
                .AddJsonFile("testSettings.json")
                .AddEnvironmentVariables("DDX_");
            var config = builder.Build();
            var appConfig = new WorkerConfiguration();
            config.Bind(appConfig);

            appConfig.ElasticsearchUrl.Should().Be("http://some.elasticsearch:9200/");
            appConfig.DatasetIndexName.Should().Be("TestDatasets");
            appConfig.JobsIndexName.Should().Be("TestJobs");
            appConfig.OwnerSettingsIndexName.Should().Be("TestOwners");
            appConfig.SchemaIndexName.Should().Be("TestSchemas");
            appConfig.RepoSettingsIndexName.Should().Be("TestRepoSettings");
            appConfig.UserIndexName.Should().Be("TestUsers");
            appConfig.FileStorePath.Should().Be("/path/to/file/store");
            appConfig.GitPath.Should().Be("/usr/bin/git");
            appConfig.GitHubClientHeader.Should().Be("MyDataDock");
            appConfig.PublishUrl.Should().Be("http://mydatadock.com/");
            appConfig.RepoBaseDir.Should().Be("/data/repos");
        }
    }

   
}
