using DataDock.Common;

namespace DataDock.Web.Config
{
    public class WebConfiguration : ApplicationConfiguration
    {
        public string AdminLogins { get; set; }
        public string OAuthClientId { get; set; }
        public string OAuthClientSecret { get; set; }
    }
}
