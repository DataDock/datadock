using DotLiquid;

namespace DataDock.Worker.Liquid
{
    public class NamespaceDrop : Drop
    {
        private readonly string _uriBase;

        public NamespaceDrop(string uriBase)
        {
            _uriBase = uriBase;
        }

        public override object BeforeMethod(string method)
        {
            return _uriBase + method;
        }
    }
}
