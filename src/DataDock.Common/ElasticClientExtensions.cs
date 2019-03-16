using System;
using System.Collections.Generic;
using System.Text;
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
            var retry = Policy.HandleResult(false)
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
                var clusterHealthResponse = client.ClusterHealth();
                if (clusterHealthResponse.IsValid)
                {
                    return clusterHealthResponse.Status != Health.Red;
                }
            }

            return false;
        }
    
}
}
