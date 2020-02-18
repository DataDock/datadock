using System;
using Elasticsearch.Net;
using Nest;
using Polly;
using Serilog;

namespace DataDock.Common
{
    public static class ElasticClientExtensions
    {
        public static void WaitForInitialization(this IElasticClient client)
        {
            var retry = Polly.Policy.HandleResult(false)
                .WaitAndRetryForever(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (result, timespan) =>
                    {
                        Log.Warning("Elasticsearch is not yet initialized. Backing off for {timespan} seconds.");
                    });
            retry.Execute(() => IsServerInitialized(client));
        }

        private static bool IsServerInitialized(IElasticClient client)
        {
            var pingResponse = client.Ping();
            if (pingResponse.IsValid)
            {
                Log.Information("Elasticsearch cluster responded to PING. Checking cluster health...");
                var clusterHealthResponse = client.ClusterHealth();
                if (clusterHealthResponse.IsValid)
                {
                    switch (clusterHealthResponse.Status) {
                        case Health.Red:
                        Log.Warning("Elasticsearch cluster health is RED. Will back off and wait for cluster to recover");
                        return false;
                        case Health.Yellow:
                            Log.Warning("Elasticsearch cluster health is YELLOW.");
                            return true;
                        case Health.Green:
                            Log.Information("Elasticsearch cluster health is GREEN");
                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return false;
        }
    
}
}
