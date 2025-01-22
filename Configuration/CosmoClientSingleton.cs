using System;
using Microsoft.Azure.Cosmos;

namespace CosmosClientSingletonconfiguration
{
    public static class CosmosClientSingleton
    {
        private static CosmosClient _cosmosClient;

        public static CosmosClient GetCosmosClient()
        {
            if (_cosmosClient == null)
            {
                string endpointUrl = Environment.GetEnvironmentVariable("CosmosDBEndpoint");
                string authorizationKey = Environment.GetEnvironmentVariable("CosmosDBKey");

                if (string.IsNullOrEmpty(endpointUrl) || string.IsNullOrEmpty(authorizationKey))
                {
                    throw new InvalidOperationException("CosmosDB endpoint or authorization key is not configured in environment variables.");
                }

                _cosmosClient = new CosmosClient(endpointUrl, authorizationKey, new CosmosClientOptions
                {
                    AllowBulkExecution = true,
                    RequestTimeout = TimeSpan.FromSeconds(120),
                    MaxRetryAttemptsOnRateLimitedRequests = 10,
                    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(20)
                });
            }

            return _cosmosClient;
        }
    }
}
