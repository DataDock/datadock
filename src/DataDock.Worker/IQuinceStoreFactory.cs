using NetworkedPlanet.Quince;

namespace DataDock.Worker
{
    /// <summary>
    /// Interface for a factory to generate an IQuinceStore wrapper for a DataDock GitHub repository
    /// </summary>
    public interface IQuinceStoreFactory
    {
        /// <summary>
        /// Create an IQuinceStore wrapper for a DataDock GitHub repository
        /// </summary>
        /// <param name="repoDirectoryPath">The path to the root directory of the DataDock GitHub repository</param>
        /// <returns></returns>
        IQuinceStore MakeQuinceStore(string repoDirectoryPath);
    }
}
