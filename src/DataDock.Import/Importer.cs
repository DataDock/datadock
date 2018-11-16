using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using DataDock.Common.Elasticsearch;
using DataDock.Common.Models;
using DataDock.Import.Models;
using Newtonsoft.Json;

namespace DataDock.Import
{
    class Importer
    {
        private Options _options;
        private DatasetStore _datasetStore;
        private SchemaStore _schemaStore;

        public Importer(Options opts, DatasetStore datasetStore, SchemaStore schemaStore)
        {
            _options = opts;
            _datasetStore = datasetStore;
            _schemaStore = schemaStore;
        }

        public async Task RunAsync()
        {
            await ImportDatasetsAsync();
            await ImportSchemasAsync();
        }

        private async Task ImportDatasetsAsync()
        {
            var datasetJson = File.ReadAllText(_options.DatasetsJsonFile);
            var datasets = JsonConvert.DeserializeObject<List<LegacyDatasetInfo>>(datasetJson);
            foreach (var ds in datasets)
            {
                await _datasetStore.CreateOrUpdateDatasetRecordAsync(new DatasetInfo
                {
                    OwnerId = ds.OwnerId,
                    RepositoryId = FixRepositoryId(ds.RepositoryId),
                    DatasetId = ds.DatasetId,
                    LastModified = ds.LastModified,
                    ShowOnHomePage = ds.ShowOnHomePage,
                    CsvwMetadata = ds.Metadata,
                    Tags = ds.Metadata["dcat:keyword"]?.ToObject<List<string>>()
                });
            }
        }

        private string FixRepositoryId(string repositoryId)
        {
            var fix = repositoryId.Contains('/') ? repositoryId.Split('/')[1] : repositoryId;
            Console.WriteLine("FixRepositoryId: {0} => {1}", repositoryId, fix);
            return fix;
        }

        private async Task ImportSchemasAsync()
        {
            var schemasJson = await File.ReadAllTextAsync(_options.SchemasJsonFile);
            var schemas = JsonConvert.DeserializeObject<List<LegacySchemaInfo>>(schemasJson);
            foreach (var s in schemas)
            {
                await _schemaStore.CreateOrUpdateSchemaRecordAsync(new SchemaInfo
                {
                    OwnerId = s.OwnerId,
                    RepositoryId = FixRepositoryId(s.RepositoryId),
                    SchemaId = s.SchemaId,
                    Schema = s.Schema
                });
            }
        }
    }
}
