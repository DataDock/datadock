namespace DataDock.Common.Stores
{
    public class OwnerSettingsNotFoundException : JobStoreException
    {
        public OwnerSettingsNotFoundException(string ownerId) : base("No owner settings found with ownerId " + ownerId) { }
    }
}