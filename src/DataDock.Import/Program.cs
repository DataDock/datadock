using System;
using System.IO;
using System.Threading.Tasks;
using DataDock.Common;
using DataDock.Common.Elasticsearch;
using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;

namespace DataDock.Import
{
    class Program
    {
        static void Main(string[] args)
        {
            Options options;
            try
            {
                options = ValidateArguments(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Usage();
                return;
            }

            try
            {
                var esClient = new ElasticClient(
                    new ConnectionSettings(
                        new SingleNodeConnectionPool(new Uri(options.ElasticsearchUrl)),
                        JsonNetSerializer.Default));
                var config = new ApplicationConfiguration {ElasticsearchUrl = options.ElasticsearchUrl};
                var datasetsStore = new DatasetStore(esClient, config);
                var schemaStore = new SchemaStore(esClient, config);
                var importer = new Importer(options, datasetsStore, schemaStore);
                importer.RunAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error running import process: " + ex);
            }
        }

        private static void Usage()
        {
            Console.Out.WriteLine("Usage: DataDock.Import datasets_file schemas_file elasticsearch_url");
            Console.Out.WriteLine("  datasets_file: Path to the JSON file containing exported datasets records");
            Console.Out.WriteLine("  schemas_file: Path to the JSON file containing exported schemas records");
            Console.Out.WriteLine("  elasticsearch_url: URL to the Elasticsearch instance to import into");
        }

        private static Options ValidateArguments(string[] args)
        {
            if (args.Length != 3) throw new Exception("Expected exactly 3 command-line arguments");
            var opts = new Options
            {
                DatasetsJsonFile = args[0],
                SchemasJsonFile = args[1],
                ElasticsearchUrl = args[2]
            };
            if (!File.Exists(opts.DatasetsJsonFile)) throw new Exception("Could not find datasets JSON file at " + opts.DatasetsJsonFile);
            if (!File.Exists(opts.SchemasJsonFile)) throw new Exception("Could not find schemas JSON file at " + opts.SchemasJsonFile);
            if (!Uri.TryCreate(opts.ElasticsearchUrl, UriKind.Absolute, out var esUri))
                throw new Exception("Invalid URL for Elasticsearch");
            return opts;
        }
    }
}
