namespace DataDock.Common.Stores
{
    public class UserAccountNotFoundException : UserStoreException
    {
        public UserAccountNotFoundException(string userId) : base($"Could not find account record for user {userId}")
        {
        }
    }
}