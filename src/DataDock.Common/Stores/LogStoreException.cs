using System;

namespace DataDock.Common.Stores
{
    public class LogStoreException : Exception
    {
        public LogStoreException(string msg) : base(msg)
        {
        }
        public LogStoreException(string msg, Exception innerException): base(msg, innerException) { }
    }
}