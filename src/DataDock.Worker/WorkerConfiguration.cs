using DataDock.Common;
using Serilog;

namespace DataDock.Worker
{
    public class WorkerConfiguration : ApplicationConfiguration
    {
        /// <summary>
        /// The path to the Git executable
        /// </summary>
        public string GitPath { get; set; } = "git";

        /// <summary>
        /// The path to the directory to use for cloning user repositories
        /// </summary>
        public string RepoBaseDir { get; set; } = "/datadock/repositories";

        /// <summary>
        /// The URL of the SignalR hub to send progress messages to
        /// </summary>
        public string SignalRHubUrl { get; set; } = "http://web/progress";

        public override void LogSettings()
        {
            base.LogSettings();
            Log.Information("Configured Publish URL {PublishUrl}", PublishUrl);
            Log.Information("Configured Git Path {GitPath}", GitPath);
            Log.Information("Configured Repository Base Directory {RepoBaseDir}", RepoBaseDir);
            Log.Information("Configured GitHub Client Header {GitHubClientHeader}", GitHubClientHeader);
            Log.Information("Configured SignalR Hub Url {SignalRHubUrl}", SignalRHubUrl);
        }
    }
}