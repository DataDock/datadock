using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataDock.CsvWeb.Metadata;
using DataDock.CsvWeb.Parsing;
using DataDock.CsvWeb.Rdf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;

namespace DataDock.Worker
{
    public class CsvProcessor : IProgress<int>
    {
        private readonly IProgressLog _progressLog;
        public static int CsvConversionReportInterval = 250;

        public CsvProcessor(IProgressLog progressLog)
        {
            _progressLog = progressLog;

        }

        public async Task<IGraph> GenerateGraphAsync(string csvPath, string metadataFilePath, Uri metadataUri, JObject metadataJson)
        {
            if (!File.Exists(csvPath))
            {
                throw new WorkerException($"Could not find CSV file at {csvPath}.", nameof(csvPath));
            }
            if (!File.Exists(metadataFilePath))
            {
                throw new WorkerException($"Could not find metadata file at {metadataFilePath}",
                    nameof(metadataFilePath));
            }
            var parser = new JsonMetadataParser(null, metadataUri);
            var tableGroup = new TableGroup();
            try
            {
                var tableMeta = parser.ParseTable(tableGroup, metadataJson);
                if (tableMeta == null)
                {
                    throw new WorkerException("CSV Conversion failed. Unable to read CSV table metadata.");
                }
            }
            catch (MetadataParseException ex)
            {
                Log.Error(ex, "Invalid CSV table metadata: " + ex.Message);
                throw new WorkerException(ex, "CSV conversion failed. Invalid CSV table metadata: " + ex.Message);
            }
            catch (JsonException ex)
            {
                Log.Error(ex, $"Error parsing metadata JSON: {ex.Message}");
                throw new WorkerException(ex,
                    $"CSV conversion failed. Unable to parse CSV Metadata JSON: {ex.Message}");
            }

            var graph = new Graph();
            _progressLog.Info("Running CSV to RDF conversion");
            var graphHandler = new GraphHandler(graph);
            var tableResolver = new LocalTableResolver(metadataUri, metadataFilePath);
            tableResolver.CacheResolvedUri(tableGroup.Tables[0].Url, csvPath);
            var converter = new Converter(graphHandler, tableResolver, ConverterMode.Minimal, (msg) => _progressLog.Error(msg), this, reportInterval: CsvConversionReportInterval);
            await converter.ConvertAsync(tableGroup);
            if (converter.Errors.Any())
            {
                foreach (var e in converter.Errors)
                {
                    _progressLog.Error(e);
                }
                throw new WorkerException("One or more errors where encountered during the CSV to RDF conversion.");
            }
            return graph;
        }

        public void Report(int value)
        {
            _progressLog.Info("CSV conversion processed {0} rows", value);
        }
    }
}
