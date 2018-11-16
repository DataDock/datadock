namespace DataDock.Common.Stores
{
    public class LogNotFoundException : LogStoreException
    {
        public LogNotFoundException(string msg) : base(msg) { }
    }
}