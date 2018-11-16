using System;

namespace DataDock.Common
{
    /// <summary>
    /// Common base-class for DD-specific exceptions
    /// </summary>
    public class DataDockException: Exception
    {
        public DataDockException(string msg) : base(msg) { }
    }
}
