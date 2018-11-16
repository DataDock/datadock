namespace DataDock.Common.Stores
{
    public class UserSettingsNotFoundException : UserStoreException
    {
        public UserSettingsNotFoundException(string userId) : base($"Could not find user settings for user {userId}")
        {
        }
    }
}