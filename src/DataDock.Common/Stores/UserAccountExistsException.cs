namespace DataDock.Common.Stores
{
    public class UserAccountExistsException : UserStoreException
    {
        public UserAccountExistsException(string userId):base($"An account already exists for user {userId}") { }
    }
}