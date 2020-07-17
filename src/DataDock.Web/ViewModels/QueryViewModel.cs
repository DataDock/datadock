using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace DataDock.Web.ViewModels
{
    public class QueryViewModel : BaseLayoutViewModel
    {
        public string[] DataSources { get; set; }

        public string DataSourcesJson
        {
            get
            {
                return "[" + string.Join(", ", DataSources.Select(ds => HttpUtility.JavaScriptStringEncode(ds, true))) + "]";
            }
        }
    }
}
