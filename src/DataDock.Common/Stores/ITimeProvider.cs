using System;

namespace DataDock.Common.Stores
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
}