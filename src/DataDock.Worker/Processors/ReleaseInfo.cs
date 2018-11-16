using System;
using System.Collections.Generic;
using System.Text;

namespace DataDock.Worker.Processors
{
    public class ReleaseInfo
    {
        public string Tag { get; private set; }
        public List<string> DownloadLinks { get; private set; }

        public ReleaseInfo(string tag)
        {
            Tag = tag;
            DownloadLinks = new List<string>();
        }
    }
}
