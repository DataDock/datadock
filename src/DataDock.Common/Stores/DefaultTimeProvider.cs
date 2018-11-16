using System;

namespace DataDock.Common.Stores
{
    public class DefaultTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}