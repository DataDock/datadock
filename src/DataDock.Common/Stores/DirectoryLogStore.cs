using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace DataDock.Common.Stores
{

    public class DirectoryLogStore : ILogStore
    {
        private readonly ITimeProvider _timeProvider;
        private readonly string _basePath;
        private readonly ILogger _log = Log.ForContext<DirectoryLogStore>();

        public DirectoryLogStore(ITimeProvider timeProvider, string basePath, int timeToLive)
        {
            _timeProvider = timeProvider;
            _basePath = basePath;
            LogTimeToLive = timeToLive;
        }

        public DirectoryLogStore(ApplicationConfiguration applicationConfiguration)
        {
            _timeProvider = new DefaultTimeProvider();
            _basePath = applicationConfiguration.LogStorePath;
            LogTimeToLive = applicationConfiguration.LogTimeToLive;
        }

        public async Task<string> AddLogAsync(string ownerId, string repoId, string jobId, string logText)
        {
            try
            {
                var logDir = _timeProvider.UtcNow.ToString("yyyyMMdd");
                var logPath = Path.Combine(logDir, jobId + ".log");
                _log.Information("AddLog {Owner}/{Repo}:{Job} at {Path}", ownerId, repoId, jobId, logPath);
                Directory.CreateDirectory(Path.Combine(_basePath, logDir));
                await File.WriteAllTextAsync(Path.Combine(_basePath, logPath), logText, Encoding.UTF8);
                return logPath;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error adding log for {Owner}/{Repo}:{Job}", ownerId, repoId, jobId);
                throw new LogStoreException("Error writing persistent log file.", ex);
            }
        }

        public async Task<string> GetLogContentAsync(string logIdentifier)
        {
            try
            {
                _log.Information("GetLog {LogId}", logIdentifier);
                var logPath = Path.Combine(_basePath, logIdentifier);
                if (!File.Exists(logPath))
                {
                    _log.Warning("Log {LogId} not found", logIdentifier);
                    throw new LogNotFoundException("Could not find persistent log " + logIdentifier);
                }

                return await File.ReadAllTextAsync(logPath, Encoding.UTF8);
            }
            catch (LogNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error retrieving log {LogId}", logIdentifier);
                throw new LogStoreException("Could not read persistent log " + logIdentifier, ex);
            }
        }

        public void PruneLogs()
        {
            _log.Information("PruneLogs Started");
            var today = _timeProvider.UtcNow;
            foreach (var dir in Directory.EnumerateDirectories(_basePath))
            {
                _log.Debug("PruneLog evaluated directory {LogDir}", dir);
                var dirDate = DateTime.ParseExact(Path.GetFileName(dir), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                _log.Debug("PruneLog evaluated log directory date as {DirDate} for {LogDir}", dirDate, dir );
                var dirAge = Math.Floor(today.Subtract(dirDate).TotalDays);
                if (dirAge > LogTimeToLive)
                {
                    try
                    {
                        _log.Information("Pruning directory {LogDir} that is {DirAge} old (> {LogTimeToLive})", dir, dirAge, LogTimeToLive );
                        Directory.Delete(dir, true);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "PruneLogs failed to delete log directory {LogDir}", dir);
                    }
                }
            }
        }

        public int LogTimeToLive { get; }
    }
}
