using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataDock.Common;
using DataDock.Common.Models;
using Medallion.Shell;
using NetworkedPlanet.Quince.Git;
using Octokit;
using Serilog;
using VDS.RDF;
using VDS.RDF.Writing;

namespace DataDock.Worker.Processors
{
    public class GitCommandProcessor
    {
        public WorkerConfiguration Configuration { get; }
        public IProgressLog ProgressLog { get; }
        private readonly IGitHubClientFactory _gitHubClientFactory;
        private readonly IGitWrapperFactory _gitWrapperFactory;

        public GitCommandProcessor(WorkerConfiguration configuration, IProgressLog progressLog, IGitHubClientFactory gitHubClientFactory, IGitWrapperFactory gitWrapperFactory)
        {
            Configuration = configuration;
            ProgressLog = progressLog;
            _gitHubClientFactory = gitHubClientFactory;
            _gitWrapperFactory = gitWrapperFactory;
        }

        public async Task CloneRepository(string repository, string targetDirectory, string authenticationToken, UserAccount userAccount)
        {
            if (!await EnsureRepository(repository, targetDirectory, authenticationToken, userAccount))
            {
                throw new WorkerException("Failed to validate remote repository {0}. Please check that the repository exists and that you have write access to it.", repository);
            }

            var cloneWrapper = _gitWrapperFactory.MakeGitWrapper(Configuration.RepoBaseDir);
            ProgressLog.Info("Cloning {0}", repository);

            var cloneResult = await cloneWrapper.Clone(repository, targetDirectory, depth: 1, branch: "gh-pages");
            if (!cloneResult.Success)
            {
                LogCommandError(cloneResult, $"Clone of repository {repository} gh-pages branch failed.");
                ProgressLog.Info("Clone of gh-pages branch failed. Attempting to clone default branch and create a new gh-pages branch");

                cloneResult = await cloneWrapper.Clone(repository, targetDirectory, depth: 1);
                if (!cloneResult.Success)
                {
                    LogCommandError(cloneResult, $"Clone of repository {repository} failed.");
                    throw new WorkerException("Clone of repository {0} failed.", repository);
                }
                var repoDir = Path.Combine(Configuration.RepoBaseDir, targetDirectory);
                var branchWrapper = _gitWrapperFactory.MakeGitWrapper(repoDir);
                var branchResult = await branchWrapper.NewBranch("gh-pages", force: true);
                if (!branchResult.Success)
                {
                    LogCommandError(cloneResult, $"Failed to create a new gh-pages branch in the repository {repository}.");
                    throw new WorkerException("Failed to create a gh-pages branch in the repository {0}", repository);
                }
                await PushChanges(repository, repoDir, authenticationToken, true);
            }
            ProgressLog.Info("Clone of {0} complete", repository);
        }

        private async Task<bool> EnsureRepository(string repository, string targetDirectory, string authenticationToken, UserAccount userAccount)
        {
            var repoDir = Path.Combine(Configuration.RepoBaseDir, targetDirectory);
            Directory.CreateDirectory(repoDir);
            Log.Information("EnsureRepository: repo={repoId}, targetDirectory={targetDir}, RepoBaseDir={baseDir}, GitPath={gitPath}", repository, targetDirectory, Configuration.RepoBaseDir, Configuration.GitPath);
            var cloneWrapper = _gitWrapperFactory.MakeGitWrapper(repoDir, Configuration.RepoBaseDir);
            var repoTarget = "https://" + authenticationToken + ":@" + repository.Substring(8);
            ProgressLog.Info("Verifying {0}", repository);
            var lsRemoteResult = await cloneWrapper.ListRemote(repository, headsOnly: true, setExitCode: true);
            if (!lsRemoteResult.Success)
            {
                ProgressLog.Warn("{0} appears to be an empty repository. Attempting to initialize it", repository);
                var gitWrapper = _gitWrapperFactory.MakeGitWrapper(repoDir);
                var initResult = await gitWrapper.Init();
                if (initResult.Success)
                {
                    using (var writer = File.CreateText(Path.Combine(repoDir, "README.md")))
                    {
                        writer.WriteLine(
                            "Created by DataDock. You can delete this file after importing your first data set.");
                    }
                    var commitResult = await CommitChanges(repoDir, "Initial commit", userAccount);
                    if (commitResult)
                    {
                        var remoteResult = await gitWrapper.AddRemote("origin", repoTarget);
                        if (remoteResult.Success)
                        {
                            var pushed = await gitWrapper.Push("master");
                            //var pushed = await PushChanges(repository, repoDir, authenticationToken, true, branch:"master");
                            if (!pushed.Success) ProgressLog.Error("Failed to push to new repository.");
                            try
                            {
                                FileSystemHelper.DeleteDirectory(repoDir);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Failed to remove temporary directory {dirPath}", repoDir);
                            }
                            return pushed.Success;
                        }
                        else
                        {
                            ProgressLog.Error("Failed to add origin remote");
                        }
                    }
                    else
                    {
                        ProgressLog.Error("Failed to add initialization file to repository");
                    }
                }
                else
                {
                    ProgressLog.Error("Failed to initialize local Git repository");
                }
                return false;
            }
            return true;
        }

        public async Task<bool> CommitChanges(string repositoryDirectory, string commitMessage, UserAccount userAccount)
        {
            var nameClaim = userAccount.Claims.FirstOrDefault(c => c.Type.Equals(ClaimTypes.Name));
            var emailClaim = userAccount.Claims.FirstOrDefault(c => c.Type.Equals(ClaimTypes.Email));
            var email = emailClaim?.Value ?? "noreply@datadock.io";
            var commitAuthor = nameClaim.Value + " <" + email + ">";

            var git = _gitWrapperFactory.MakeGitWrapper(repositoryDirectory);
            ProgressLog.Info("Adding files to git repository.");
            var configResult = await git.SetUserName(nameClaim.Value);
            if (!configResult.Success)
            {
                throw new WorkerException("Commit failed: Could not configure git user name");
            }
            configResult = await git.SetUserEmail(email);
            if (!configResult.Success)
            {
                throw new WorkerException("Commit failed: Could not configure git user email");
            }

            var addResult = await git.AddAll();
            if (!addResult.Success)
            {
                ProgressLog.Error("git-add command failed with exit code {0}. Detail: {1}\n{2}", addResult.ExitCode, addResult.StandardOutput, addResult.StandardError);
                if (!string.IsNullOrEmpty(addResult.StandardOutput))
                {
                    ProgressLog.Info("git-add command output: {0}", addResult.StandardOutput);
                }
                if (!string.IsNullOrEmpty(addResult.StandardError))
                {
                    ProgressLog.Info("git-add command error output: {0}", addResult.StandardError);
                }
                throw new WorkerException("Commit failed: Failed to add modified files to local git working tree.");
            }

            var statusResult = await git.Status();
            if (!statusResult.Success)
            {
                ProgressLog.Error("git-status command failed with exit code {0}", statusResult.CommandResult.ExitCode);
                if (!string.IsNullOrEmpty(statusResult.CommandResult.StandardOutput))
                {
                    ProgressLog.Info("git-status command output: {0}", statusResult.CommandResult.StandardOutput);
                }
                if (!string.IsNullOrEmpty(statusResult.CommandResult.StandardError))
                {
                    ProgressLog.Info("git-status command error output: {0}", statusResult.CommandResult.StandardError);
                }
                throw new WorkerException("Commit failed: Could not determine current state of local git working tree.");
            }

            // Commit only if there are some changes
            if (statusResult.DeletedFiles.Any() || statusResult.ModifiedFiles.Any() || statusResult.NewFiles.Any())
            {
                var commitResult = await git.Commit(subject: commitMessage, author: commitAuthor);
                if (!commitResult.Success)
                {
                    ProgressLog.Error("git-commit command failed with exit code {0}.", commitResult.ExitCode,
                        commitResult.StandardOutput, commitResult.StandardError);
                    if (!string.IsNullOrEmpty(commitResult.StandardOutput))
                    {
                        ProgressLog.Info("git-commit command output: {0}", commitResult.StandardOutput);
                    }
                    if (!string.IsNullOrEmpty(commitResult.StandardError))
                    {
                        ProgressLog.Info("git-commit command error output: {0}", commitResult.StandardError);
                    }
                    throw new WorkerException("Commit failed: Git commit failed.");
                }
            }
            else
            {
                ProgressLog.Info("No changes to commit");
                return false;
            }
            return true;
        }

        public async Task PushChanges(string remoteUrl, string repositoryDirectory, string authenticationToken, bool setUpstream = false, string branch = "gh-pages")
        {
            try
            {
                var gitWrapper = _gitWrapperFactory.MakeGitWrapper(repositoryDirectory, Configuration.GitPath);
                var repoTarget = "https://" + authenticationToken + ":@" + remoteUrl.Substring(8);
                var pushResult = await gitWrapper.PushTo(repoTarget, branch, setUpstream);
                if (!pushResult.Success)
                {
                    ProgressLog.Error("Failed to push to remote repository.");
                    throw new WorkerException("Failed to push to remote repository.");
                }
            }
            catch (WorkerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ProgressLog.Exception(ex, "Failed to push gh-pages branch to GitHub.");
                throw new WorkerException(ex, "Failed to push to remote repository.");
            }
        }

        public async Task<ReleaseInfo> MakeRelease(IGraph dataGraph, string releaseTag, string owner, string repositoryId, string datasetId, string repositoryDirectory, string authenticationToken)
        {
            var releaseInfo = new ReleaseInfo(releaseTag);
            var ntriplesDumpFileName = Path.Combine(repositoryDirectory, releaseTag + ".nt.gz");
            ProgressLog.Info("Generating gzipped NTriples data dump");
            var writer = new GZippedNTriplesWriter();
            writer.Save(dataGraph, ntriplesDumpFileName);

            // Make a release
            try
            {
                ProgressLog.Info("Generating a new release of dataset {0}", datasetId);
                if (authenticationToken == null) throw new WorkerException("No valid GitHub access token found for your account.");
                var client = _gitHubClientFactory.CreateClient(authenticationToken);
                client.SetRequestTimeout(TimeSpan.FromSeconds(300));
                var releaseClient = client.Repository.Release;
                var newRelease = new NewRelease(releaseTag) { TargetCommitish = "gh-pages" };
                var release = await releaseClient.Create(owner, repositoryId, newRelease);

                // Attach data dump file(s) to release
                try
                {
                    ProgressLog.Info("Uploading data dump files to GitHub release");
                    using (var zipFileStream = File.OpenRead(ntriplesDumpFileName))
                    {
                        var upload = new ReleaseAssetUpload(Path.GetFileName(ntriplesDumpFileName), "application/gzip",
                            zipFileStream, null);
                        var releaseAsset = await releaseClient.UploadAsset(release, upload);
                        releaseInfo.DownloadLinks.Add(releaseAsset.BrowserDownloadUrl);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to attach dump files to GitHub release");
                    throw new WorkerException(ex, "Failed to attach dump files to GitHub release");
                }
            }
            catch (WorkerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create a new GitHub release");
                throw new WorkerException(ex, "Failed to create a new GitHub release");
            }
            return releaseInfo;
        }

        private void LogCommandError(CommandResult commandResult, string errorMessage)
        {
            Log.Error("Command exited with code {exitCode}. Stdout: {stdout}. Stderr: {stderr}", commandResult.ExitCode, commandResult.StandardOutput, commandResult.StandardError);
            ProgressLog.Error($"{errorMessage} Exit code was: {commandResult.ExitCode}. Command output follows.");
            ProgressLog.Error("Clone command stdout: {0}", commandResult.StandardOutput);
            ProgressLog.Error("Clone command stderr: {0}", commandResult.StandardError);
        }

    }
}
