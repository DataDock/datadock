using DataDock.Common.Elasticsearch;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class RepoSettingsStoreTests : IClassFixture<ElasticsearchFixture>
    {
        private readonly RepoSettingsStore _store;

        public RepoSettingsStoreTests(ElasticsearchFixture fixture)
        {
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
            var retrievedRepoSettings = await _store.GetRepoSettingsAsync("owner-1", "repo-1");
            retrievedRepoSettings.Id.Should().Be("owner-1/repo-1");
            retrievedRepoSettings.OwnerId.Should().Be("owner-1");
            retrievedRepoSettings.RepositoryId.Should().Be("repo-1");
            (retrievedRepoSettings.OwnerIsOrg).Should().BeFalse();
            retrievedRepoSettings.LastModified.Should().BeCloseTo(repoSettings.LastModified);

            var retrievedByIdRepoSettings = await _store.GetRepoSettingsByIdAsync("owner-1/repo-1");
            retrievedByIdRepoSettings.Id.Should().Be("owner-1/repo-1");
            retrievedByIdRepoSettings.OwnerId.Should().Be("owner-1");
            retrievedByIdRepoSettings.RepositoryId.Should().Be("repo-1");
            (retrievedByIdRepoSettings.OwnerIsOrg).Should().BeFalse();
            retrievedByIdRepoSettings.LastModified.Should().BeCloseTo(repoSettings.LastModified);
        }

    }

    public class RepoSettingsStoreFixture : ElasticsearchFixture
    {
        public RepoSettingsStore Store { get; }
        public RepoSettingsStoreFixture()
        {
            Store = new RepoSettingsStore(Client, Configuration);
            InitializeRepository().Wait();
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
            results.OwnerId.Should().Be("owner-0");
            results.RepositoryId.Should().Be("repo-0");
        }
        [Fact]
        public void ItShouldReturnNoResultsByOwnerWhenNoneExist()
        {
            var ex = Assert.ThrowsAsync<RepoSettingsNotFoundException>(async () =>
                await _store.GetRepoSettingsForOwnerAsync("owner-100"));

            Assert.Equal("No repo settings found with ownerId owner-100", ex.Result.Message);
        }


        [Fact]
        public async void ItCanRetrieveRepoSettingsForSingleRepository()
        {
            var results = await _store.GetRepoSettingsAsync("owner-0", "repo-0");
            results.Should().NotBeNull();
            results.OwnerId.Should().Be("owner-0");
            results.RepositoryId.Should().Be("repo-0");
        }

        [Fact]
        public void ItShouldReturnNoResultsByRepositoryIdWhenNoneExist()
        {
            var ex = Assert.ThrowsAsync<RepoSettingsNotFoundException>(async () =>
                await _store.GetRepoSettingsAsync("owner-0", "repo-100"));

            Assert.Equal("No repo settings found with ownerId owner-0 and repositoryId repo-100", ex.Result.Message);
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
