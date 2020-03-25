using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace DataDock.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((webHostBuilderContext, configurationbuilder) =>
                {
                    var environment = webHostBuilderContext.HostingEnvironment;
                    var baseAppSettingsFile = Path.Combine(environment.ContentRootPath, "appsettings.json");
                    var environmentAppSettingsFile = Path.Combine(environment.ContentRootPath,
                        $"appsettings.{environment.EnvironmentName.ToLower()}.json");
                    var logSettingsFile = Path.Combine(environment.ContentRootPath, "logsettings.json");
                    var environmentLogSettingsFile = Path.Combine(environment.ContentRootPath,
                        $"logsettings.{environment.EnvironmentName.ToLower()}.json");
                    configurationbuilder
                        .AddJsonFile(baseAppSettingsFile, optional: true)
                        .AddJsonFile(environmentAppSettingsFile, optional: true)
                        .AddJsonFile(logSettingsFile, true)
                        .AddJsonFile(environmentLogSettingsFile, true)
                        .AddEnvironmentVariables("DD_")
                        .AddEnvironmentVariables("DD_" + environment + "_");
                })
                .UseStartup<Startup>()
                .Build();
    }
}
