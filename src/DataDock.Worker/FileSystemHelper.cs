using System.IO;

namespace DataDock.Worker
{
    /// <summary>
    /// Recursively deletes a directory and its contents
    /// </summary>
    /// <remarks>Handles the case where a directory/file needs its file attributes to be reset for the deletion to work.
    /// This is a particular problem in working with Git repository directories under Windows.</remarks>
    public static class FileSystemHelper
    {
        public static void DeleteDirectory(string directory)
        {
            SetAttributesNormal(new DirectoryInfo(directory));
            Directory.Delete(directory, true);
        }

        private static void SetAttributesNormal(DirectoryInfo dir)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                subDir.Attributes = FileAttributes.Normal;
                SetAttributesNormal(subDir);
            }
            foreach (var file in dir.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
            }
        }
    }
}
