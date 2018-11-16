using System.Text;
using VDS.RDF;
using VDS.RDF.Writing.Formatting;

namespace DataDock.Worker
{
    public class NQuadFormatter
    {
        private readonly INodeFormatter _formatter;

        public NQuadFormatter()
        {
            _formatter = new NQuads11Formatter();
        }

        public string FormatQuad(Triple t)
        {
            var line = new StringBuilder();
            line.Append(_formatter.Format(t.Subject));
            line.Append(' ');
            line.Append(_formatter.Format(t.Predicate));
            line.Append(' ');
            line.Append(_formatter.Format(t.Object));
            line.Append(" <");
            line.Append(t.GraphUri);
            line.Append(">.");
            return line.ToString();
        }
    }
}
