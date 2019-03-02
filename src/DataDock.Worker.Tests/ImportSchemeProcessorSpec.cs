using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using DataDock.Worker.Processors;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class ImportSchemeProcessorSpec
    {
        private readonly Mock<ISchemaStore> _mockSchemeStore;
        private readonly Mock<IFileStore> _mockFileStore;
        private readonly Mock<IProgressLog> _mockProgressLog;

        public ImportSchemeProcessorSpec()
        {
            _mockSchemeStore = new Mock<ISchemaStore>();
            _mockFileStore = new Mock<IFileStore>();
            _mockProgressLog = new Mock<IProgressLog>();
        }

        private void WithJsonSchema(string jsonString)
        {
            _mockFileStore.Setup(fs => fs.GetFileAsync(It.IsAny<string>()))
                .ReturnsAsync(() => new MemoryStream(Encoding.UTF8.GetBytes(jsonString)));
        }

        [Fact]
        public async void ItWrapsTheSchemaInSourceMetadata()
        {
            var config = new WorkerConfiguration { PublishUrl = "http://datadock.io/" };
            var proc = new ImportSchemaProcessor(config, _mockSchemeStore.Object, _mockFileStore.Object);
            WithJsonSchema(@"{ '@context': 'http://www.w3.org/ns/csvw' }");
            var job = new JobInfo
            {
                JobType = JobType.SchemaCreate,
                UserId = "kal",
                OwnerId = "datadock",
                RepositoryId = "test",
                SchemaFileId = "schemaFileId"
            };
            await proc.ProcessJob(job, new UserAccount(), _mockProgressLog.Object);
            _mockSchemeStore.Verify(s => s.CreateOrUpdateSchemaRecordAsync(It.Is<SchemaInfo>(p =>
                p.OwnerId.Equals("datadock") &&
                p.RepositoryId.Equals("test") &&
                p.Schema.ContainsKey("@context") &&
                (p.Schema["@context"] as JValue).Value<string>().Equals("http://www.w3.org/ns/csvw"))));
        }

        [Fact]
        public async Task ItMakesUrlPropertiesRelative()
        {
            var config = new WorkerConfiguration {PublishUrl = "http://datadock.io/"};
            var proc = new ImportSchemaProcessor(config, _mockSchemeStore.Object, _mockFileStore.Object);
            WithJsonSchema(@"{
                '@context': 'http://www.w3.org/ns/csvw',
                'url': 'http://datadock.io/datadock/test/id/dataset/mydataset',
                'dc:title': 'http://datadock.io/datadock/test/foo',
                'dc:license': 'https://creativecommons.org/publicdomain/zero/1.0/',
                'aboutUrl': 'http://datadock.io/datadock/test/id/resource/mydataset/monument_record_no/{monument_record_no}', 
                'tableSchema': {
                    'columns': [
                        {
                            'name' : 'foo',
                            'propertyUrl': 'http://datadock.io/datadock/test/id/definition/foo',
                            'valueUrl' : 'http://datadock.io/datadock/test/id/bar/{bar}'
                        }
                    ]
                }
            }");
            var job = new JobInfo
            {
                JobType = JobType.SchemaCreate, UserId = "kal", OwnerId = "datadock", RepositoryId = "test",
                SchemaFileId = "schemaFileId"
            };
            await proc.ProcessJob(job, new UserAccount(), _mockProgressLog.Object);
            _mockSchemeStore.Verify(s=>s.CreateOrUpdateSchemaRecordAsync(It.Is<SchemaInfo>(p=>
                (p.Schema["aboutUrl"] as JValue).Value<string>().Equals("id/resource/mydataset/monument_record_no/{monument_record_no}"))));
            _mockSchemeStore.Verify(s => s.CreateOrUpdateSchemaRecordAsync(It.Is<SchemaInfo>(p =>
                (p.Schema["dc:title"] as JValue).Value<string>().Equals("http://datadock.io/datadock/test/foo"))));
            _mockSchemeStore.Verify(s => s.CreateOrUpdateSchemaRecordAsync(It.Is<SchemaInfo>(p =>
                (p.Schema["tableSchema"]["columns"][0]["propertyUrl"] as JValue).Value<string>()
                .Equals("id/definition/foo"))));
            _mockSchemeStore.Verify(s => s.CreateOrUpdateSchemaRecordAsync(It.Is<SchemaInfo>(p =>
                (p.Schema["tableSchema"]["columns"][0]["valueUrl"] as JValue).Value<string>()
                .Equals("id/bar/{bar}"))));
        }
    }
}
