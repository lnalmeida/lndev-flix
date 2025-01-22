using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using PostToDatabase;
using CosmosClientSingletonconfiguration;
using lndev_flix.Models;

namespace GetMovieDetails
{
    public static class fnGetMovieDetails
    {
        [FunctionName("fnGetMovieDetails")]
        public static async Task<object> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger logger)
        {
            string databaseName = Environment.GetEnvironmentVariable("DatabaseName");
            string containerName = Environment.GetEnvironmentVariable("ContainerName");
            string partitionKey = "/id";

            logger.LogInformation("Proccessing request to retrive data from CosmosDB.");

            try
            {
                CosmosClient cosmosClient = CosmosClientSingleton.GetCosmosClient();
                Container container = cosmosClient.GetContainer(databaseName, containerName);

                string id = req.Query["id"];
                if (string.IsNullOrEmpty(id))
                {
                    logger.LogWarning("Request does not contain an ID.");
                    return new BadRequestObjectResult("Please provide an ID in the query string.");
                }

                ItemResponse<MovieResponse> response = await container.ReadItemAsync<MovieResponse>(id, new PartitionKey(id));

                if (response == null)
                {
                    logger.LogWarning("No movie found with the provided ID.");
                    return new NotFoundObjectResult("No movie found with the provided ID.");
                }

                MovieResponse movieResponse = response.Resource;

                return new OkObjectResult(movieResponse);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error: {ex.Message}");
                return new BadRequestObjectResult("Error: " + ex.Message);
            }
        }
    }
}
