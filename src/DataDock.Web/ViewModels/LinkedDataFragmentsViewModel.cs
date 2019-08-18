using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
        public IGraph ResultsGraph { get; }
        public IGraph MetadataGraph { get; }

        public LinkedDataFragmentsViewModel(string owner, string repo, string s, string p, string o, IGraph resultsGraph, IGraph metadataGraph)
        {
            Title = "Linked Data Fragments";
            Heading = "Linked Data Fragments";
            Subject = s;
            Predicate = p;
            Object = o;
            Owner = owner;
            Repo = repo;
            ResultsGraph = resultsGraph;
            MetadataGraph = metadataGraph;
        }


        public string Pattern => (Subject ?? "?s") + " " + (Predicate ?? "?p") + " " + (Object ?? "?o");
    }
}
