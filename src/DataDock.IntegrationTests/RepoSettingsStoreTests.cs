using DataDock.Common.Elasticsearch;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class RepoSettingsStoreTests : IClassFixture<ElasticsearchFixture>
    {
        private readonly ElasticsearchFixture _fixture;
        private readonly RepoSettingsStore _store;

        public RepoSettingsStoreTests(ElasticsearchFixture fixture)
        {
            _fixture = fixture;
            _store = new RepoSettingsStore(fixture.Client, fixture.Configuration);
        }

        [Fact]
        public async Task ItCanCreateAndRetrieveRepoSettings()
        {
            var repoSettings = new RepoSettings
            {
                OwnerId = "owner-1",
                RepositoryId = "repo-1",
                OwnerIsOrg = false,
                LastModified = DateTime.UtcNow
            };

            await _store.CreateOrUpdateRepoSettingsAsync(repoSettings);
            Thread.Sleep(1000);
            var retrievedRepoSettings = await _store.GetRepoSettingsAsync("owner-1", "repo-1");
            retrievedRepoSettings.Id.Should().Be($"owner-1/repo-1");
            ((string)retrievedRepoSettings.OwnerId).Should().Be("owner-1");
            ((string)retrievedRepoSettings.RepositoryId).Should().Be("repo-1");
            (retrievedRepoSettings.OwnerIsOrg).Should().BeFalse();
            retrievedRepoSettings.LastModified.Should().BeCloseTo(repoSettings.LastModified);

            var retrievedByIdRepoSettings = await _store.GetRepoSettingsByIdAsync("owner-1/repo-1");
            retrievedByIdRepoSettings.Id.Should().Be($"owner-1/repo-1");
            ((string)retrievedByIdRepoSettings.OwnerId).Should().Be("owner-1");
            ((string)retrievedByIdRepoSettings.RepositoryId).Should().Be("repo-1");
            (retrievedByIdRepoSettings.OwnerIsOrg).Should().BeFalse();
            retrievedByIdRepoSettings.LastModified.Should().BeCloseTo(repoSettings.LastModified);
        }

    }

    public class RepoSettingsStoreFixture : ElasticsearchFixture
    {
        public RepoSettingsStore Store { get; }
        public RepoSettingsStoreFixture() : base()
        {
            Store = new RepoSettingsStore(Client, Configuration);
            InitializeRepository().Wait();
            Thread.Sleep(1000);
        }

        private async Task InitializeRepository()
        {
            for (var o = 0; o < 5; o++)
            {
                for (var r = 0; r < 5; r++)
                {
                    var repoSettings = new RepoSettings
                    {
                        OwnerId = "owner-" + o,
                        RepositoryId = "repo-" + r,
                        LastModified = DateTime.UtcNow
                    };

                    await Store.CreateOrUpdateRepoSettingsAsync(repoSettings);
                }
            }

        }

    }

    public class RepoSettingsStoreSearchTests : IClassFixture<RepoSettingsStoreFixture>
    {
        private readonly RepoSettingsStore _store;

        public RepoSettingsStoreSearchTests(RepoSettingsStoreFixture fixture)
        {
            _store= fixture.Store;
        }


        [Fact]
        public async void ItCanRetrieveMultipleRepoSettingsForASingleOwner()
        {
            var results = await _store.GetRepoSettingsAsync("owner-0", "repo-0");
            results.Should().NotBeNull();
            var rs = results as RepoSettings;
            rs.Should().NotBeNull();
            rs.OwnerId.Should().Be("owner-0");
            rs.RepositoryId.Should().Be("repo-0");
        }
        [Fact]
        public void ItShouldReturnNoResultsByOwnerWhenNoneExist()
        {
            var ex = Assert.ThrowsAsync<RepoSettingsNotFoundException>(async () =>
                await _store.GetRepoSettingsForOwnerAsync("owner-100"));

            Assert.Equal($"No repo settings found with ownerId owner-100", ex.Result.Message);
        }


        [Fact]
        public async void ItCanRetrieveRepoSettingsForSingleRepository()
        {
            var results = await _store.GetRepoSettingsAsync("owner-0", "repo-0");
            results.Should().NotBeNull();
            var rs = results as RepoSettings;
            rs.Should().NotBeNull();
            rs.OwnerId.Should().Be("owner-0");
            rs.RepositoryId.Should().Be("repo-0");
        }

        [Fact]
        public void ItShouldReturnNoResultsByRepositoryIdWhenNoneExist()
        {
            var ex = Assert.ThrowsAsync<RepoSettingsNotFoundException>(async () =>
                await _store.GetRepoSettingsAsync("owner-0", "repo-100"));

            Assert.Equal($"No repo settings found with ownerId owner-0 and repositoryId repo-100", ex.Result.Message);
        }

        [Fact]
        public async void ItCanRetrieveRepoSettingsById()
        {
            var result = await _store.GetRepoSettingsByIdAsync("owner-0/repo-0");
            result.Should().NotBeNull();
            result.OwnerId.Should().Be("owner-0");
            result.RepositoryId.Should().Be("repo-0");
        }
    }
}
