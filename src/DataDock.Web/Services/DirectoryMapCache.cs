using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace DataDock.Web.Services
{
    public class DirectoryMapCache
    {
        public IMemoryCache Cache { get; set; }

        public DirectoryMapCache()
        {
            Cache = new MemoryCache(new MemoryCacheOptions{SizeLimit = 512*1024});
        }
    }
}
