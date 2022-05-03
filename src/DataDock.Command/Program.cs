// See https://aka.ms/new-console-template for more information

using CommandLine;
using DataDock.Command;
using DataDock.Worker;

Parser.Default.ParseArguments<ImportOptions, DeleteOptions>(args)
    .MapResult(
        async (ImportOptions opts) => await RunImportAsync(opts),
        async (DeleteOptions opts) => await RunDeleteAsync(opts),
        async errors => await Task.FromResult(-1)
    );

async Task<int> RunImportAsync(ImportOptions opts)
{
    return await new ImportCommand(opts, new ConsoleProgressLog()).Run();
}

async Task<int> RunDeleteAsync(DeleteOptions opts)
{
    return 0;
}