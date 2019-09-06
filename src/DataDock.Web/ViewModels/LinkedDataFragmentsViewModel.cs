using VDS.RDF;

namespace DataDock.Web.ViewModels
{
    public class LinkedDataFragmentsViewModel : BaseLayoutViewModel
    {
        public string Subject { get; }
        public string Predicate { get; }
        public string Object { get; }
        public string Owner { get; }
        public string Repo { get; }
        public string Dataset { get; }
        public IGraph ResultsGraph { get; }
        public IGraph MetadataGraph { get; }
        public string Source { get; }

        public LinkedDataFragmentsViewModel(string owner, string repo, string dataset, string s, string p, string o, IGraph resultsGraph, IGraph metadataGraph)
        {
            Title = "Linked Data Fragments";
            Heading = "Linked Data Fragments";
            Subject = s;
            Predicate = p;
            Object = o;
            Owner = owner;
            Repo = repo;
            Dataset = dataset;

            ResultsGraph = resultsGraph;
            MetadataGraph = metadataGraph;

            Source = Owner + "/" + Repo;
            if (!string.IsNullOrEmpty(Dataset)) Source += "/" + Dataset;
        }

        public string Pattern => (Subject ?? "?s") + " " + (Predicate ?? "?p") + " " + (Object ?? "?o");
        
    }
}
