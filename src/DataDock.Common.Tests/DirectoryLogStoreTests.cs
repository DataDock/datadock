using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DataDock.Common.Stores;
using Moq;
using Xunit;

namespace DataDock.Common.Tests
{
    public class DirectoryLogStoreTests
    {
        [Fact]
        public async Task ItCreatesADateBasedDirectoryAndJobIdBasedFile()
        {
            var timeProvider = new Mock<ITimeProvider>();
            timeProvider.SetupGet(t => t.UtcNow)
                .Returns(new DateTime(2018, 5, 9, 12, 01, 01, DateTimeKind.Utc));
            var store = new DirectoryLogStore(timeProvider.Object, Path.Combine("data", "logs"), 90);
            var logId = await store.AddLogAsync("kal", "test", "0123456789", "This is a log file!");
            var expectDir = Path.Combine("data", "logs", "20180509");
            var expectLog = Path.Combine(expectDir, "0123456789.log");

            Assert.NotNull(logId);
            Assert.True(Directory.Exists(expectDir), "Expected a log directory to be created for date 20180509");
            Assert.True(File.Exists(expectLog));
            var logContent = await File.ReadAllTextAsync(expectLog);
            Assert.Equal("This is a log file!", logContent);
        }

        [Fact]
        public async Task ItWritesMultipleLogsOnTheSameDayToTheSameDirectory()
        {
            var timeProvider = new Mock<ITimeProvider>();
            timeProvider.SetupSequence(t => t.UtcNow)
                .Returns(new DateTime(2018, 5, 10, 12, 01, 01, DateTimeKind.Utc))
                .Returns(new DateTime(2018, 5, 10, 12, 02, 00, DateTimeKind.Utc));
            var store = new DirectoryLogStore(timeProvider.Object, Path.Combine("data", "logs"), 90);
            var log1Id = await store.AddLogAsync("kal", "test", "0123456789", "This is a log file!");
            var log2Id = await store.AddLogAsync("jen", "other", "9876543210", "This is a different log file!");
            var expectDir = Path.Combine("data", "logs", "20180510");
            var expectLog1 = Path.Combine(expectDir, "0123456789.log");
            var expectLog2 = Path.Combine(expectDir, "9876543210.log");

            Assert.True(Directory.Exists(expectDir));
            Assert.True(File.Exists(expectLog1));
            Assert.True(File.Exists(expectLog2));
            Assert.Equal("This is a log file!", await store.GetLogContentAsync(log1Id));
            Assert.Equal("This is a different log file!", await store.GetLogContentAsync(log2Id));
        }

        [Fact]
        public async Task ItWritesLogsOnDifferentDaysToDifferentDirectories()
        {
            var timeProvider = new Mock<ITimeProvider>();
            timeProvider.SetupSequence(t => t.UtcNow)
                .Returns(new DateTime(2018, 5, 11, 12, 01, 01, DateTimeKind.Utc))
                .Returns(new DateTime(2018, 5, 12, 12, 02, 00, DateTimeKind.Utc));
            var store = new DirectoryLogStore(timeProvider.Object, Path.Combine("data", "logs"), 90);
            var log1Id = await store.AddLogAsync("kal", "test", "0123456789", "This is a log file!");
            var log2Id = await store.AddLogAsync("jen", "other", "9876543210", "This is a different log file!");
            var expectDir1 = Path.Combine("data", "logs", "20180511");
            var expectDir2 = Path.Combine("data", "logs", "20180512");
            var expectLog1 = Path.Combine(expectDir1, "0123456789.log");
            var expectLog2 = Path.Combine(expectDir2, "9876543210.log");

            Assert.True(Directory.Exists(expectDir1));
            Assert.True(Directory.Exists(expectDir2));
            Assert.True(File.Exists(expectLog1));
            Assert.True(File.Exists(expectLog2));
            Assert.Equal("This is a log file!", await store.GetLogContentAsync(log1Id));
            Assert.Equal("This is a different log file!", await store.GetLogContentAsync(log2Id));
        }

        [Fact]
        public async Task ItPrunesOldDirectories()
        {
            var timeProvider = new Mock<ITimeProvider>();
            var now = DateTime.UtcNow;
            timeProvider.SetupSequence(t => t.UtcNow)
                .Returns(now.Subtract(TimeSpan.FromDays(89)))
                .Returns(now.Subtract(TimeSpan.FromDays(90)))
                .Returns(now.Subtract(TimeSpan.FromDays(91)))
                .Returns(now);

            var job1Id = Guid.NewGuid().ToString("N");
            var job2Id = Guid.NewGuid().ToString("N");
            var job3Id = Guid.NewGuid().ToString("N");
            var store = new DirectoryLogStore(timeProvider.Object, Path.Combine("data", "logs"), 90);
            var log1Id = await store.AddLogAsync("kal", "test", job1Id, "This is a log file!");
            var log2Id = await store.AddLogAsync("jen", "other", job2Id, "This is a different log file!");
            var log3Id = await store.AddLogAsync("kal", "test", job3Id, "This is a third log file!");

            // Before pruning, all logs should be accesible
            Assert.Equal("This is a log file!", await store.GetLogContentAsync(log1Id));
            Assert.Equal("This is a different log file!", await store.GetLogContentAsync(log2Id));
            Assert.Equal("This is a third log file!", await store.GetLogContentAsync(log3Id));

            store.PruneLogs();

            // After pruning log3 should be deleted
            await Assert.ThrowsAsync<LogNotFoundException>(() => store.GetLogContentAsync(log3Id));
            // log2 should still be available (its age matches TTL)
            Assert.Equal("This is a different log file!", await store.GetLogContentAsync(log2Id));
            // log1 should still be available (its age is < TTL)
            Assert.Equal("This is a log file!", await store.GetLogContentAsync(log1Id));

        }
    }
}
