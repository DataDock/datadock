using System;
using System.Text;
using DataDock.Common.Models;

namespace DataDock.Worker.Tests
{
    public class MockProgressLog : IProgressLog
    {
        private readonly StringBuilder _builder;

        public MockProgressLog()
        {
            _builder = new StringBuilder();
        }

        public void UpdateStatus(JobStatus newStatus, string progressMessage, params object[] args)
        {
            Console.WriteLine("Update Status: {0} {1}", newStatus, string.Format(progressMessage, args));
            _builder.AppendFormat(progressMessage, args);
        }

        public void Info(string infoMessage, params object[] args)
        {
            Console.WriteLine("Info: " + infoMessage, args);
            _builder.AppendFormat(infoMessage, args);
        }

        public void Warn(string warnMessage, params object[] args)
        {
            Console.WriteLine("Warn: " + warnMessage, args);
            _builder.AppendFormat(warnMessage, args);
        }

        public void Error(string errorMessage, params object[] args)
        {
            Console.WriteLine("Error: " + errorMessage, args);
            _builder.AppendFormat(errorMessage, args);
        }

        public void Exception(Exception exception, string errorMessage, params object[] args)
        {
            Console.WriteLine("Exception: " + errorMessage, args);
            Console.WriteLine("Exception Detail: " + exception);
            _builder.AppendFormat(errorMessage, args);
        }

        public string GetLogText()
        {
            return _builder.ToString();
        }
    }
}