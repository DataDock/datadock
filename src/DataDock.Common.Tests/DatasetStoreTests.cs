using System;
using System.Threading;
using DataDock.Common.Elasticsearch;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using Moq;
using Nest;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DataDock.Common.Tests
{
    public class DatasetStoreTests
    {
        private readonly string _indexName = "datasets";

        [Fact]
        public void StoreCreatesIndexIfItDoesNotExist()
        {
            var client = new Mock<IElasticClient>();
            var notExists = new Mock<IExistsResponse>();
            notExists.SetupGet(x => x.Exists).Returns(false);
            var indexCreated = new Mock<ICreateIndexResponse>();
            indexCreated.SetupGet(x => x.Acknowledged).Returns(true);
            client.Setup(x => x.IndexExists(It.IsAny<Indices>(), null)).Returns(notExists.Object);
            client.Setup(x => x.CreateIndex(_indexName, It.IsAny<Func<CreateIndexDescriptor, ICreateIndexRequest>>()))
                .Returns(indexCreated.Object).Verifiable();
            var indexDict = new FluentDictionary<Type, string>();
            var connectionSettings = new Mock<IConnectionSettingsValues>();
            connectionSettings.Setup(x => x.DefaultIndices).Returns(indexDict).Verifiable();
            client.SetupGet(x => x.ConnectionSettings).Returns(connectionSettings.Object).Verifiable();

            var datasetStore = new DatasetStore(client.Object, new ApplicationConfiguration{DatasetIndexName = _indexName});
            client.Verify();
        }

        [Fact]
        public async void CreateDatasetRequiresNonNullDatasetInfo()
        {
            var client = new Mock<IElasticClient>();
            AssertIndexExists(client, _indexName);
            var datasetStore = new DatasetStore(client.Object, new ApplicationConfiguration {DatasetIndexName = _indexName});
            await Assert.ThrowsAsync<ArgumentNullException>(() => datasetStore.CreateOrUpdateDatasetRecordAsync(null));
        }

        [Fact]
        public async void CreateDatasetInsertsIntoDatasetsIndex()
        {
            var mockResponse = new Mock<IIndexResponse>();
            mockResponse.SetupGet(x => x.IsValid).Returns(true);
            var client = new Mock<IElasticClient>();
            AssertIndexExists(client, _indexName);

            client.Setup(x => x.IndexDocumentAsync<DatasetInfo>(It.IsAny<DatasetInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object).Verifiable();

            var datasetStore = new DatasetStore(client.Object, new ApplicationConfiguration{DatasetIndexName = _indexName});

            var csvwJson = new JObject(new JProperty("dc:title", "Test Dataset"), new JProperty("dcat:keyword", new JArray("one", "two", "three")));
            var voidJson = new JObject(
                new JProperty("void:triples", "100"),
                new JProperty("void:dataDump", new JArray("https://github.com/jennet/animated-barnacle/releases/download/acsv_csv_20180207_170200/acsv_csv_20180207_170200.nt.gz", "http://datadock.io/jennet/animated-barnacle/csv/acsv.csv/acsv.csv")));
            var datasetInfo = new DatasetInfo
            {
                OwnerId = "git-user",
                RepositoryId = "repo-name",
                DatasetId = "test.csv",
                ShowOnHomePage = true,
                LastModified = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
                CsvwMetadata = csvwJson,
                VoidMetadata = voidJson
            };
            Assert.Null(datasetInfo.Id);

            var created = await datasetStore.CreateOrUpdateDatasetRecordAsync(datasetInfo);

            client.Verify();
            Assert.NotNull(created);
            Assert.Equal($"{datasetInfo.OwnerId}/{datasetInfo.RepositoryId}/{datasetInfo.DatasetId}", datasetInfo.Id);
        }

        [Fact]
        public async void CreateDatasetThrowsWhenInsertFails()
        {
            var mockResponse = new Mock<IIndexResponse>();
            mockResponse.SetupGet(x => x.IsValid).Returns(false);
            var client = new Mock<IElasticClient>();
            AssertIndexExists(client, _indexName);
            client.Setup(x => x.IndexDocumentAsync<DatasetInfo>(It.IsAny<DatasetInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object).Verifiable();
            var datasetStore = new DatasetStore(client.Object, new ApplicationConfiguration{DatasetIndexName = _indexName});

            var emptyDatasetInfo = new DatasetInfo();

            await Assert.ThrowsAsync<DatasetStoreException>(() => datasetStore.CreateOrUpdateDatasetRecordAsync(emptyDatasetInfo));

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
