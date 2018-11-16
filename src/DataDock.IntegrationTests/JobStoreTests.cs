using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataDock.Common.Elasticsearch;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Common;
using FluentAssertions;
using Nest;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class JobStoreTests : IClassFixture<ElasticsearchFixture>, IDisposable
    {
        private readonly JobStore _repo;
        private readonly ElasticsearchFixture _fixture;

        public JobStoreTests(ElasticsearchFixture esFixture)
        {
            _fixture = esFixture;
            _repo = new JobStore(_fixture.Client, _fixture.Configuration);
        }

        public void Dispose()
        {

        }

        [Fact]
        public async void CanCreateAndRetrieveAnImportJob()
        {
            await _fixture.Client.DeleteByQueryAsync<JobInfo>(s=>s.MatchAll());
            var jobInfo = await _repo.SubmitImportJobAsync(new ImportJobRequestInfo
            {
                UserId = "user",
                OwnerId = "owner",
                RepositoryId = "repo",
                DatasetId = "dataset",
                DatasetIri = "http://datadock.io/owner/repo/dataset",
                CsvFileName = "data.csv",
                CsvFileId = "fileid1",
                CsvmFileId = "fileid2",
                IsPublic = true,
                OverwriteExistingData = false
            });
            Assert.NotNull(jobInfo.JobId);
            Assert.NotEmpty(jobInfo.JobId);
            Assert.True(jobInfo.QueuedTimestamp > 0);
            Assert.True(jobInfo.QueuedTimestamp <= DateTime.UtcNow.Ticks);

            var retrievedJobInfo = await _repo.GetJobInfoAsync(jobInfo.JobId);
            Assert.Equal("user", retrievedJobInfo.UserId);
            Assert.Equal("owner", retrievedJobInfo.OwnerId);
            Assert.Equal("repo", retrievedJobInfo.RepositoryId);
            Assert.Equal("fileid1", jobInfo.CsvFileId);
            Assert.Equal("fileid2", jobInfo.CsvmFileId);
        }

    }

    public class JobStoreFixture : ElasticsearchFixture
    {
        public JobStore Store { get; }
        public JobStoreFixture() : base()
        {
            Store = new JobStore(Client, Configuration);
            InitializeRepository().Wait();
            Thread.Sleep(1000);
        }

        private async Task InitializeRepository()
        {
            for (var o = 0; o < 5; o++)
            {
                for (var r = 0; r < 5; r++)
                {
                    var request = new ImportJobRequestInfo
                    {
                        UserId = "user",
                        OwnerId = "owner_" + o,
                        RepositoryId = "repo_" + r,
                        DatasetId = "dataset",
                        DatasetIri = $"http://datadock.io/owner_{o}/repo_{r}/dataset",
                        CsvFileName = "data.csv",
                        CsvFileId = "fileid",
                        CsvmFileId = "fileid",
                        IsPublic = true,
                        OverwriteExistingData = false
                    };
                    await Store.SubmitImportJobAsync(request);
                }
            }

        }

    }

    public class JobStoreSearchTests : IClassFixture<JobStoreFixture>
    {
        private readonly JobStore _store;

        public JobStoreSearchTests(JobStoreFixture fixture)
        {
            _store = fixture.Store;
        }


        [Fact]
        public async void ItCanRetrieveJobsForASingleOwner()
        {
            var results = await _store.GetJobsForOwner("owner_0");
            results.Should().NotBeNull();
            results.Count().Should().Be(5);
            foreach (var r in results)
            {
                r.OwnerId.Should().Be("owner_0");
            }

        }

        [Fact]
        public async void ItCanRetrieveJobsForASingleRepository()
        {
            var results = await _store.GetJobsForRepository("owner_0", "repo_0");
            results.Should().NotBeNull();
            results.Count().Should().Be(1);
            foreach (var r in results)
            {
                r.OwnerId.Should().Be("owner_0");
                r.RepositoryId.Should().Be("repo_0");
            }

        }

        [Fact]
        public void ItThrowsNotFoundExceptionForASingleOwnerWhenNoneExist()
        {
            var ex = Assert.ThrowsAsync<JobNotFoundException>(async () =>
                await _store.GetJobsForOwner("owner_100"));

            Assert.StartsWith($"No jobs found for ownerId owner_100", ex.Result.Message);
        }

        [Fact]
        public async void ItThrowsNotFoundExceptionForASingleRepositoryWhenNoneExist()
        {
           var ex = Assert.ThrowsAsync<JobNotFoundException>(async () =>
                await _store.GetJobsForRepository("owner_0", "repo_100"));

            Assert.StartsWith($"No jobs found for ownerId owner_0 and repositoryId repo_100", ex.Result.Message);
        }
    }
}
