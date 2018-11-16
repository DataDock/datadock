using DataDock.Common.Elasticsearch;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class DatasetStoreTests : IClassFixture<ElasticsearchFixture>
    {
        private readonly ElasticsearchFixture _fixture;
        private readonly DatasetStore _store;

        public DatasetStoreTests(ElasticsearchFixture fixture)
        {
            _fixture = fixture;
            _store = new DatasetStore(fixture.Client, fixture.Configuration);
        }

        [Fact]
        public async Task ItCanCreateAndRetrieveDataset()
        {
            var csvwJson = new JObject(new JProperty("dc:title", "Test Dataset"), new JProperty("dcat:keyword", new JArray("one", "two", "three")));
            var voidJson = new JObject(
                new JProperty("void:triples", "100"),
                new JProperty("void:dataDump", new JArray("https://github.com/jennet/animated-barnacle/releases/download/acsv_csv_20180207_170200/acsv_csv_20180207_170200.nt.gz", "http://datadock.io/jennet/animated-barnacle/csv/acsv.csv/acsv.csv")));
            var datasetInfo = new DatasetInfo
            {
                OwnerId = "owner-1",
                RepositoryId = "repo-1",
                DatasetId = "test.csv",
                ShowOnHomePage = true,
                LastModified = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
                CsvwMetadata = csvwJson,
                VoidMetadata = voidJson
            };

            await _store.CreateOrUpdateDatasetRecordAsync(datasetInfo);
            Thread.Sleep(1000);
            var retrievedDataset = await _store.GetDatasetInfoAsync("owner-1", "repo-1", "test.csv");
            retrievedDataset.Id.Should().Be($"owner-1/repo-1/test.csv");
            ((string)retrievedDataset.OwnerId).Should().Be("owner-1");
            ((string)retrievedDataset.RepositoryId).Should().Be("repo-1");
            ((string)retrievedDataset.DatasetId).Should().Be("test.csv");
            retrievedDataset.LastModified.Should().BeCloseTo(datasetInfo.LastModified);

            var retrievedByIdDataset = await _store.GetDatasetInfoByIdAsync("owner-1/repo-1/test.csv");
            retrievedByIdDataset.Id.Should().Be($"owner-1/repo-1/test.csv");
            ((string)retrievedByIdDataset.OwnerId).Should().Be("owner-1");
            ((string)retrievedByIdDataset.RepositoryId).Should().Be("repo-1");
            ((string)retrievedByIdDataset.DatasetId).Should().Be("test.csv");
            retrievedByIdDataset.LastModified.Should().BeCloseTo(datasetInfo.LastModified);

            var retrievedVoid = retrievedByIdDataset.VoidMetadata;
            var tripleCount = retrievedVoid["void:triples"].Value<string>();
            Assert.Equal("100", tripleCount);
        }

    }

    public class DatasetStoreFixture : ElasticsearchFixture
    {
        public DatasetStore Store { get; }
        public DatasetStoreFixture() : base()
        {
            Store = new DatasetStore(Client, Configuration);
            InitializeStore().Wait();
            Thread.Sleep(1000);
        }

        private async Task InitializeStore()
        {
            var count = 0;
            for (var o = 0; o < 5; o++)
            {
                for (var r = 0; r < 5; r++)
                {
                    for (var d = 0; d < 5; d++)
                    {
                        count++;
                        var tags = new List<string> {"test", $"owner-{o}", $"repo-{r}", $"set-{d}"};
                        if (d == 0)
                        {
                            tags.Add("foo");
                        }
                        var csvwJson = new JObject(new JProperty("dc:title", $"Test Dataset {d} (Owner {o} Repo {r})"), new JProperty("dcat:keyword", new JArray(tags)));
                        var voidJson = new JObject(
                            new JProperty("void:triples", "100"),
                            new JProperty("void:dataDump", 
                                new JArray(
                                    $"https://github.com/owner-{o}/repo-{r}/releases/download/test-{d}_csv_20180207_170200/test-{d}_csv_20180207_170200.nt.gz", 
                                    $"http://datadock.io/owner-{o}/repo-{r}/csv/test-{d}.csv/test-{d}.csv")));

                        // 4 of the 5 datasets will have showOnHomepage = true, the final dataset will be showOnHomepage = false
                        var datasetInfo = new DatasetInfo
                        {
                            OwnerId = $"owner-{o}",
                            RepositoryId = $"repo-{r}",
                            DatasetId = $"test-{d}.csv",
                            ShowOnHomePage = d < 4,
                            LastModified = DateTime.UtcNow.Subtract(TimeSpan.FromDays(d)),
                            CsvwMetadata = csvwJson,
                            VoidMetadata = voidJson,
                            Tags = tags
                        };
                        await Store.CreateOrUpdateDatasetRecordAsync(datasetInfo);
                    }
                }
            }
            Console.WriteLine($"{count} datasets created");
        }

    }

    public class DatasetStoreSearchTests : IClassFixture<DatasetStoreFixture>
    {
        private readonly DatasetStore _store;

        public DatasetStoreSearchTests(DatasetStoreFixture fixture)
        {
            _store= fixture.Store;
        }

        [Fact]
        public async void ItCanRetrieveAllRecentlyUpdatedDatasets()
        {
            var results = await _store.GetRecentlyUpdatedDatasetsAsync(10000, true);
            results.Should().NotBeNull();
            results.Count().Equals(125);
        }

        [Fact]
        public async void ItCanRetrieveTop50RecentlyUpdatedDatasets()
        {
            var results = await _store.GetRecentlyUpdatedDatasetsAsync(50, true);
            results.Should().NotBeNull();
            results.Count().Equals(50);
        }

        [Fact]
        public async void ItCanRetrieveTop20RecentlyUpdatedDatasetsInOrder()
        {
            var results = await _store.GetRecentlyUpdatedDatasetsAsync(20, true);
            results.Should().NotBeNull();
            results.Count().Equals(20);
            var dateCheck = DateTime.UtcNow;
            foreach (var item in results)
            {
                // first item should be more recent than next item in list
                Assert.True(item.LastModified < dateCheck);
                dateCheck = item.LastModified;
            }
        }

        [Fact]
        public async void ItCanRetrieveAllRecentlyUpdatedVisibleDatasets()
        {
            var results = await _store.GetRecentlyUpdatedDatasetsAsync(10000, false);
            results.Should().NotBeNull();
            results.Count().Equals(100);
        }

        [Fact]
        public async void ItCanRetrieveRecentlyUpdatedDatasetsForOwners()
        {
            var ownerIds = new string[] {"owner-0", "owner-1"};
            var results = await _store.GetRecentlyUpdatedDatasetsForOwnersAsync(ownerIds, 0, 1000, true);
            results.Should().NotBeNull();
            results.Count().Equals(50);
        }

        [Fact]
        public async void ItCanRetrieveRecentlyUpdatedVisibleDatasetsForOwners()
        {
            var ownerIds = new string[] { "owner-0", "owner-1" };
            var results = await _store.GetRecentlyUpdatedDatasetsForOwnersAsync(ownerIds, 0, 1000, false);
            results.Should().NotBeNull();
            results.Count().Equals(40);
        }

        [Fact]
        public async void ItCanRetrieveTop20RecentlyUpdatedDatasetsForOwners()
        {
            var ownerIds = new string[] { "owner-0", "owner-1" };
            var results = await _store.GetRecentlyUpdatedDatasetsForOwnersAsync(ownerIds, 0, 20, true);
            results.Should().NotBeNull();
            results.Count().Equals(20);
        }

        [Fact]
        public async void ItCanRetrieveTop20RecentlyUpdatedDatasetsForOwnersInOrder()
        {
            var ownerIds = new string[] { "owner-0", "owner-1" };
            var results = await _store.GetRecentlyUpdatedDatasetsForOwnersAsync(ownerIds, 0, 20, true);
            results.Should().NotBeNull();
            results.Count().Equals(20);
            var dateCheck = DateTime.UtcNow;
            foreach (var item in results)
            {
                // first item should be more recent than next item in list
                Assert.True(item.LastModified < dateCheck);
                dateCheck = item.LastModified;
            }
        }


        [Fact]
        public async void ItCanRetrieveRecentlyUpdatedDatasetsForRepositories()
        {
            var repoIds = new string[] { "repo-0", "repo-1", "repo-2" };
            var results = await _store.GetRecentlyUpdatedDatasetsForRepositoriesAsync("owner-0", repoIds, 0, 1000, true);
            results.Should().NotBeNull();
            results.Count().Equals(15);
        }

        [Fact]
        public async void ItCanRetrieveRecentlyUpdatedVisibleDatasetsForRepositories()
        {
            var repoIds = new string[] { "repo-0", "repo-1", "repo-2" };
            var results = await _store.GetRecentlyUpdatedDatasetsForRepositoriesAsync("owner-0", repoIds, 0, 1000, false);
            results.Should().NotBeNull();
            results.Count().Equals(12);
        }

        [Fact]
        public async void ItCanRetrieveTop10RecentlyUpdatedDatasetsForRepositories()
        {
            var repoIds = new string[] { "repo-0", "repo-1", "repo-2" };
            var results = await _store.GetRecentlyUpdatedDatasetsForRepositoriesAsync("owner-0", repoIds, 0, 10, true);
            results.Should().NotBeNull();
            results.Count().Equals(15);
        }

        [Fact]
        public async void ItCanRetrieveTop10RecentlyUpdatedDatasetsForRepositoriesInOrder()
        {
            var repoIds = new string[] { "repo-0", "repo-1", "repo-2" };
            var results = await _store.GetRecentlyUpdatedDatasetsForRepositoriesAsync("owner-0", repoIds, 0, 10, true);
            results.Should().NotBeNull();
            results.Count().Equals(15);
            var dateCheck = DateTime.UtcNow;
            foreach (var item in results)
            {
                // first item should be more recent than next item in list
                Assert.True(item.LastModified < dateCheck);
                dateCheck = item.LastModified;
            }
        }

        [Fact]
        public async void ItCanRetrieveMultipleDatasetsForASingleOwner()
        {
            var results = await _store.GetDatasetsForOwnerAsync("owner-0", 0, 1000);
            results.Should().NotBeNull();
            results.Count().Equals(25);
        }

        [Fact]
        public async void ItCanRetrieveOnlyVisibleDatasetsForASingleOwner()
        {
            var results = await _store.GetDatasetsForOwnerAsync("owner-0", 0, 1000);
            results.Should().NotBeNull();
            results.Count().Equals(25);
        }

        
        [Fact]
        public void ItShouldReturnNoResultsByOwnerWhenNoneExist()
        {
            var ex = Assert.ThrowsAsync<DatasetNotFoundException>(async () =>
                await _store.GetDatasetsForOwnerAsync("owner-100", 0 , 1000));

            Assert.StartsWith($"No dataset found for owner 'owner-100'", ex.Result.Message);
        }


        [Fact]
        public async void ItCanRetrieveDatasetsForSingleRepository()
        {
            var results = await _store.GetDatasetsForRepositoryAsync("owner-0", "repo-0", 0, 1000);
            results.Should().NotBeNull();
            results.Count().Equals(5);
        }

        [Fact]
        public void ItShouldReturnNoResultsByRepositoryIdWhenNoneExist()
        {
            var ex = Assert.ThrowsAsync<DatasetNotFoundException>(async () =>
                await _store.GetDatasetsForRepositoryAsync("owner-0", "repo-100", 0, 1000));

            Assert.StartsWith($"No datasets found for owner 'owner-0', repository: 'repo-100'", ex.Result.Message);
        }

        [Fact]
        public async void ItCanRetrieveDatasetById()
        {
            var result = await _store.GetDatasetInfoByIdAsync("owner-0/repo-0/test-0.csv");
            result.Should().NotBeNull();
            result.OwnerId.Should().Be("owner-0");
            result.RepositoryId.Should().Be("repo-0");
            result.DatasetId.Should().Be("test-0.csv");
        }

        [Fact]
        public async void ItCanRetrieveAllDatasetsByTag()
        {
            var tags = new string[] { "test" };
            var result = await _store.GetDatasetsForTagsAsync(tags, 0, 200, true, true); // all datasets have the 'test' tag
            result.Should().NotBeNull();
            result.Count().Should().Be(125);
        }

        [Fact]
        public async void ItCanRetrieveDatasetsByTag()
        {
            var tags = new string[] { "set-0" };
            var result = await _store.GetDatasetsForTagsAsync(tags, 0, 200, false, true); // all datasets have the 'test' tag
            result.Should().NotBeNull();
            // 5 owners x 5 repos x 1 dataset = 25
            result.Count().Should().Be(25);
        }

        [Fact]
        public async void ItCanRetrieveOwnerDatasetsByTagsCanContain()
        {
            var tags = new string[] {"repo-0", "set-0"}; 
            var result = await _store.GetDatasetsForTagsAsync("owner-0", tags, 0, 200, false, true); 
            result.Should().NotBeNull();
            // repo-0 = 5, set-0 = 5 (1 is both repo-0 and set-0) = 9
            result.Count().Should().Be(9);
        }

        [Fact]
        public async void ItCanRetrieveOwnerDatasetsByTagsMustContain()
        {
            var tags = new string[] { "repo-0", "set-0" }; 
            var result = await _store.GetDatasetsForTagsAsync("owner-0", tags, 0, 200, true, true); 
            result.Should().NotBeNull();
            result.Count().Should().Be(1);
        }

        [Fact]
        public async void ItCanRetrieveRepoDatasetsByTagsCanContain()
        {
            var tags = new string[] { "set-3", "foo" };
            var result = await _store.GetDatasetsForTagsAsync("owner-0", "repo-0", tags, 0, 200, false, true);
            result.Should().NotBeNull();
            result.Count().Should().Be(2);
        }

        [Fact]
        public async void ItCanRetrieveRepoDatasetsByTagsMustContain()
        {
            var tags = new string[] { "set-0", "foo" };
            var result = await _store.GetDatasetsForTagsAsync("owner-0", "repo-0", tags, 0, 200, true, true);
            result.Should().NotBeNull();
            result.Count().Should().Be(1);
        }

        [Fact]
        public void ItCanRetrieveRepoDatasetsByTagsMustContainNoResults()
        {
            var tags = new string[] { "set-3", "foo" };
            var ex = Assert.ThrowsAsync<DatasetNotFoundException>(async () =>
                await _store.GetDatasetsForTagsAsync("owner-0", "repo-0", tags, 0, 200, true, true));

            Assert.StartsWith($"No datasets found for repository owner-0/repo-0 with tags set-3, foo", ex.Result.Message);
        }
    }
}
