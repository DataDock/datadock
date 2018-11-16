namespace DataDock.Common.Stores
{
    public class SchemaStoreException : DataDockException
    {
        public SchemaStoreException(string msg) : base(msg) { }
    }

    public class SchemaNotFoundException : SchemaStoreException
    {
        public SchemaNotFoundException(string ownerId, string schemaId) :
            base($"Could not find schema with ID {schemaId} for owner {ownerId}")
        { }
    }
}
