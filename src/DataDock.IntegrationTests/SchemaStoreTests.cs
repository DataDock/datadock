using DataDock.Common.Elasticsearch;
using DataDock.Common.Models;
using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using DataDock.Common.Stores;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class SchemaStoreTests : IClassFixture<ElasticsearchFixture>
    {
        private readonly ElasticsearchFixture _fixture;
        private readonly SchemaStore _repo;
        private readonly JObject _dummySchema;

        public SchemaStoreTests(ElasticsearchFixture fixture)
        {
            _fixture = fixture;
            _repo = new SchemaStore(fixture.Client, fixture.Configuration);
            _dummySchema = JObject.Parse("{foo : 'foo',bar : {baz : 'baz'}}");
        }

        [Fact]
        public async Task ItCanCreateAndRetrieveASimpleSchema()
        {
            var schemaInfo = new SchemaInfo
            {
                OwnerId = "the_owner",
                RepositoryId = "the_repo",
                LastModified = DateTime.UtcNow,
                SchemaId = "the_schema_id",
                Schema = _dummySchema
            };
            await _repo.CreateOrUpdateSchemaRecordAsync(schemaInfo);
            Thread.Sleep(1000);
            var retrievedSchema = await _repo.GetSchemaInfoAsync("the_owner", "the_schema_id");
            retrievedSchema.Id.Should().Be("the_owner/the_repo/the_schema_id");
            Assert.Equal("foo", retrievedSchema.Schema["foo"].Value<string>());
            Assert.Equal("baz", retrievedSchema.Schema["bar"]["baz"].Value<string>());
            retrievedSchema.LastModified.Should().BeCloseTo(schemaInfo.LastModified);
        }

        [Fact]
        public void ItCannotCreateSchemaWithoutOwnerId()
        {
            var schemaInfo = new SchemaInfo
            {
                RepositoryId = "the_repo",
                LastModified = DateTime.UtcNow,
                SchemaId = "the_schema_id",
                Schema = _dummySchema
            };
            var ex = Assert.ThrowsAsync<ValidationException>(async () =>
                await _repo.CreateOrUpdateSchemaRecordAsync(schemaInfo));

            Assert.StartsWith($"Invalid schema info: 'OwnerId'", ex.Result.Message);
        }

        [Fact]
        public void ItCannotCreateSchemaWithoutRepoId()
        {
            var schemaInfo = new SchemaInfo
            {
                OwnerId = "the_owner",
                LastModified = DateTime.UtcNow,
                SchemaId = "the_schema_id",
                Schema = new JObject(new JProperty("foo", "foo"), new JProperty("bar", new JObject(new JProperty("baz", "baz"))))
            };
            var ex = Assert.ThrowsAsync<ValidationException>(async () =>
                await _repo.CreateOrUpdateSchemaRecordAsync(schemaInfo));

            Assert.StartsWith($"Invalid schema info: 'RepositoryId'", ex.Result.Message);
        }

        [Fact]
        public void ItCannotCreateSchemaWithoutSchemaId()
        {
            var schemaInfo = new SchemaInfo
            {
                OwnerId = "the_owner",
                RepositoryId = "the_repo",
                LastModified = DateTime.UtcNow,
                Schema = _dummySchema
            };
            var ex = Assert.ThrowsAsync<ValidationException>(async () =>
                await _repo.CreateOrUpdateSchemaRecordAsync(schemaInfo));

            Assert.StartsWith($"Invalid schema info: 'SchemaId'", ex.Result.Message);
        }

    }

    public class SchemaStoreFixture : ElasticsearchFixture
    {
        public SchemaStore Store { get; }
        public SchemaStoreFixture() : base()
        {
            Store = new SchemaStore(Client, Configuration);
            InitializeRepository().Wait();
            Thread.Sleep(1000);
        }

        private async Task InitializeRepository()
        {
            for (var o = 0; o < 5; o++)
            {
                for (var r = 0; r < 5; r++)
                {
                    var schemaInfo = new SchemaInfo
                    {
                        OwnerId = "owner-" + o,
                        RepositoryId = "repo-" + r,
                        SchemaId = "schema_" + o + "." + r,
                        LastModified = DateTime.UtcNow,
                        Schema = new JObject(new JProperty("foo", "foo"))
                    };
                    await Store.CreateOrUpdateSchemaRecordAsync(schemaInfo);
                }
            }

        }

    }

    public class SchemaStoreSearchTests : IClassFixture<SchemaStoreFixture>
    {
        private readonly SchemaStore _repo;

        public SchemaStoreSearchTests(SchemaStoreFixture fixture)
        {
            _repo= fixture.Store;
        }


        [Fact]
        public void ItCanRetrieveMultipleSchemasForASingleOwner()
        {
            var results = _repo.GetSchemasByOwnerList(new string[] {"owner-0"}, 0, 10);
            results.Count.Should().Be(5);
            foreach (var r in results)
            {
                r.OwnerId.Should().Be("owner-0");
                r.SchemaId.Should().StartWith("schema_0.");
            }
        }

        [Fact]
        public void ItCanRetrieveMultipleSchemasForMultipleOwners()
        {
            var results = _repo.GetSchemasByOwnerList(new[] {"owner-1", "owner-2"}, 0, 10);
            results.Count.Should().Be(10);
            foreach (var r in results)
            {
                r.OwnerId.Should().BeOneOf("owner-1", "owner-2");
            }
        }

        [Fact]
        public void ItCanRetrieveMultipleSchemasForMultipleOwnersWithSkip()
        {
            var results = _repo.GetSchemasByOwnerList(new[] { "owner-1", "owner-2" }, 5, 10);
            results.Count.Should().Be(5);
            foreach (var r in results)
            {
                r.OwnerId.Should().BeOneOf("owner-1", "owner-2");
            }
        }

        [Fact]
        public void ItCanRetrieveMultipleSchemasForMultipleOwnersWithSkipAndTake()
        {
            var results = _repo.GetSchemasByOwnerList(new[] { "owner-1", "owner-2" }, 5, 3);
            results.Count.Should().Be(3);
            foreach (var r in results)
            {
                r.OwnerId.Should().BeOneOf("owner-1", "owner-2");
            }
        }

        [Fact]
        public void ItCanRetrieveMultipleSchemasForMultipleRepositories()
        {
            var results =
                _repo.GetSchemasByRepositoryList("owner-1", new[] {"repo-0", "repo-1", "repo-2"}, 0, 10);
            results.Count.Should().Be(3);
            foreach (var r in results)
            {
                r.OwnerId.Should().Be("owner-1");
                r.RepositoryId.Should().BeOneOf("repo-0", "repo-1", "repo-2");
            }
        }

    }
}
