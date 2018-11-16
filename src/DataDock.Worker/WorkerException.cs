using System;

namespace DataDock.Worker
{
    public class WorkerException : Exception
    {
        public WorkerException(string fmt, params object[] args) : base(string.Format(fmt, args)) { }
        public WorkerException(Exception innerException, string fmt, params object[] args) : base(string.Format(fmt, args), innerException) { }
    }
}