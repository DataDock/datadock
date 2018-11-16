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

        public override void LogSettings()
        {
            base.LogSettings();
            Log.Information("Configured Publish URL {PublishUrl}", PublishUrl);
            Log.Information("Configured Git Path {GitPath}", GitPath);
            Log.Information("Configured Repository Base Directory {RepoBaseDir}", RepoBaseDir);
            Log.Information("Configured GitHub Client Header {GitHubClientHeader}", GitHubClientHeader);
        }
    }
}