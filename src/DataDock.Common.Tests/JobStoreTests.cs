using System;
using System.Threading;
using DataDock.Common.Elasticsearch;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using Moq;
using Nest;
using Xunit;

namespace DataDock.Common.Tests
{
    public class JobStoreTests
    {
        [Fact]
        public void RepositoryCreatesIndexIfItDoesNotExist()
        {
            var client = new Mock<IElasticClient>();
            var notExists = new Mock<IExistsResponse>();
            notExists.SetupGet(x => x.Exists).Returns(false);
            var indexCreated = new Mock<ICreateIndexResponse>();
            indexCreated.SetupGet(x => x.Acknowledged).Returns(true);
            client.Setup(x => x.IndexExists(It.IsAny<Indices>(), null)).Returns(notExists.Object);
            client.Setup(x => x.CreateIndex("jobs", It.IsAny<Func<CreateIndexDescriptor, ICreateIndexRequest>>()))
                .Returns(indexCreated.Object).Verifiable();
            var indexDict = new FluentDictionary<Type, string>();
            var connectionSettings = new Mock<IConnectionSettingsValues>();
            connectionSettings.Setup(x => x.DefaultIndices).Returns(indexDict).Verifiable();
            client.SetupGet(x => x.ConnectionSettings).Returns(connectionSettings.Object).Verifiable();

            var jobStore = new JobStore(client.Object, new ApplicationConfiguration{JobsIndexName = "jobs"});
            client.Verify();
        }

        [Fact]
        public async void SubmitImportJobRequiresNonNullRequestInfo()
        {
            var client = new Mock<IElasticClient>();
            AssertIndexExists(client, "jobs");
            var jobStore = new JobStore(client.Object, new ApplicationConfiguration { JobsIndexName = "jobs" });
            await Assert.ThrowsAsync<ArgumentNullException>(() => jobStore.SubmitImportJobAsync(null));
        }

        [Fact]
        public async void SubmitImportJobInsertsIntoJobsIndex()
        {
            var mockResponse = new Mock<IIndexResponse>();
            mockResponse.SetupGet(x => x.IsValid).Returns(true);
            var client = new Mock<IElasticClient>();
            AssertIndexExists(client, "jobs");
            client.Setup(x => x.IndexDocumentAsync<JobInfo>(It.IsAny<JobInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object).Verifiable();
            var jobStore = new JobStore(client.Object, new ApplicationConfiguration { JobsIndexName = "jobs" });
                
            var jobRequest = new ImportJobRequestInfo
            {
                UserId = "user",
                OwnerId = "owner",
                RepositoryId = "repo",
                DatasetId = "dataset",
                DatasetIri = "https://datadock.io/owner/repo/dataset",
                CsvFileName = "dataset.csv",
                CsvFileId = "csvfileid",
                CsvmFileId = "csvmfileid",
                IsPublic = true,
                OverwriteExistingData = false
            };

            var jobInfo = await jobStore.SubmitImportJobAsync(jobRequest);

            client.Verify();
        }

        [Fact]
        public async void SubmitJobThrowsWhenInsertFails()
        {
            var mockResponse = new Mock<IIndexResponse>();
            mockResponse.SetupGet(x => x.IsValid).Returns(false);
            var client = new Mock<IElasticClient>();
            AssertIndexExists(client, "jobs");
            client.Setup(x => x.IndexDocumentAsync<JobInfo>(It.IsAny<JobInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object).Verifiable();
            var jobStore = new JobStore(client.Object, new ApplicationConfiguration { JobsIndexName = "jobs" });

            var jobRequest = new ImportJobRequestInfo
            {
                UserId = "user",
                OwnerId = "owner",
                RepositoryId = "repo",
                DatasetId = "dataset",
                DatasetIri = "https://datadock.io/owner/repo/dataset",
                CsvFileName = "dataset.csv",
                CsvFileId = "csvfileid",
                CsvmFileId = "csvmfileid",
                IsPublic = true,
                OverwriteExistingData = false
            };

            await Assert.ThrowsAsync<JobStoreException>(() => jobStore.SubmitImportJobAsync(jobRequest));

            client.Verify();
        }

        private static void AssertIndexExists(Mock<IElasticClient> client, string indexName)
        {
            var indexExistsResult = new Mock<IExistsResponse>();
            indexExistsResult.SetupGet(x => x.Exists).Returns(true);

            client.Setup(x => x.IndexExists(It.IsAny<Indices>(), null))
                .Returns(indexExistsResult.Object);
            var connectionSettings = new Mock<IConnectionSettingsValues>();
            var indexDict = new FluentDictionary<Type, string>();
            connectionSettings.Setup(x=>x.DefaultIndices).Returns(indexDict).Verifiable();
            client.SetupGet(x => x.ConnectionSettings).Returns(connectionSettings.Object).Verifiable();
        }
    }
}
