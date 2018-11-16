using System;
using System.IO;
using System.Threading.Tasks;
using DataDock.Common;

namespace DataDock.Common.Stores
{
    public class DirectoryFileStore:IFileStore
    {
        private readonly string _root;

        public DirectoryFileStore(ApplicationConfiguration config) : this(config.FileStorePath) { }

        public DirectoryFileStore(string directoryPath)
        {
            _root = Path.GetFullPath(directoryPath);
            if (!Directory.Exists(_root))
            {
                Directory.CreateDirectory(_root);
            };
        }

        public async Task<string> AddFileAsync(Stream file)
        {
            var fileId = Guid.NewGuid().ToString("N");
            var filePath = Path.Combine(_root, fileId);
            using (var fileStream = File.OpenWrite(filePath))
            {
                await file.CopyToAsync(fileStream);
            }

            return fileId;
        }

        public Task<Stream> GetFileAsync(string fileId)
        {
            var filePath = Path.Combine(_root, fileId);
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Could not find file with ID: {fileId}");
            return Task.FromResult((Stream)File.OpenRead(filePath));
        }

        public Task DeleteFileAsync(string fileId)
        {
            var filePath = Path.Combine(_root, fileId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }
    }
}
