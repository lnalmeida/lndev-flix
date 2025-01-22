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
using CosmosClientSingletonconfiguration;
using lndev_flix.Models;
using System.Collections.Generic;

namespace GetAllMovies
{
    public static class fnGetAllMovies
    {
        [FunctionName("fnGetAllMovies")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger logger)
        {
            string databaseName = Environment.GetEnvironmentVariable("DatabaseName");
            string containerName = Environment.GetEnvironmentVariable("ContainerName");

            logger.LogInformation("Proccessing request to retrive all data from CosmosDB.");

            try
            {
                CosmosClient cosmosClient = CosmosClientSingleton.GetCosmosClient();
                Container container = cosmosClient.GetContainer(databaseName, containerName);

                var query = "SELECT * FROM c";
                var iterator = container.GetItemQueryIterator<MovieResponse>(query);

                List<MovieResponse> movies = [];
                while (iterator.HasMoreResults)
                {
                    FeedResponse<MovieResponse> response = await iterator.ReadNextAsync();
                    movies.AddRange(response);
                }

                return new OkObjectResult(movies);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error: {ex.Message}");
                return new BadRequestObjectResult("Error: " + ex.Message);
            }
        }
    }
}
