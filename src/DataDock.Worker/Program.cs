using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DataDock.Worker
{
    internal class Program
    {
        private static readonly AutoResetEvent WaitHandle = new AutoResetEvent(false);

        static void Main(string[] args)
        {
            Log.Information("Worker Starting");
            var environment = Environment.GetEnvironmentVariable("DD_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables("DD_");
            if (!string.IsNullOrEmpty(environment))
            {
                builder.AddJsonFile("appsettings." + environment.ToLowerInvariant() + ".json");
            }
            var configuration = builder.Build();
            var workerConfig = new WorkerConfiguration();
            configuration.Bind(workerConfig);
            workerConfig.LogSettings();
            var serviceCollection = new ServiceCollection();
            var startup = new Startup();
            startup.ConfigureServices(serviceCollection, workerConfig);
            var services = serviceCollection.BuildServiceProvider();
            var application = new Application(services);

            Task.Run(application.Run);

            Console.CancelKeyPress += (o, e) =>
            {
                Console.WriteLine("Exit");
                WaitHandle.Set();
            };

            WaitHandle.WaitOne();
        }
    }
}
