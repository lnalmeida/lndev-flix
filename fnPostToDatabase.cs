#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using lndev_flix.Models;

namespace PostToDatabase;

public static class PostToDatabase
{
    private static readonly CosmosClient cosmosClient = CosmosClientSingleton.GetCosmosClient();

    [FunctionName("fnPostToDatabase")]
    public static async Task<object?> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequest req,
        ILogger logger)
    {
        string databaseName = Environment.GetEnvironmentVariable("DatabaseName");
        string containerName = Environment.GetEnvironmentVariable("ContainerName");
        string PartitionKey = "/id";
        logger.LogInformation("Processing request to send data to Cosmos DB.");

        try
        {
            CosmosClient cosmosClient = CosmosClientSingleton.GetCosmosClient();
            DatabaseResponse database = cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName).Result;
            logger.LogInformation("Cosmos DB database obtained.");
            Container container = await database.Database.CreateContainerIfNotExistsAsync(
                id: containerName,
                partitionKeyPath: PartitionKey
            );
            logger.LogInformation("Cosmos DB container obtained.");

            string content = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrEmpty(content))
            {
                logger.LogWarning("Request body is empty");
                return new BadRequestObjectResult("Request body cannot be empty");
            }

            logger.LogInformation("Request body is not empty");
            MovieRequest? movieRequest = JsonConvert.DeserializeObject<MovieRequest>(content);

            if (movieRequest == null)
            {
                logger.LogWarning("Failed to deserialize request body.");
                return new BadRequestObjectResult("Invalid JSON format.");
            }

            if (string.IsNullOrEmpty(movieRequest.Id))
            {
                movieRequest.Id = Guid.NewGuid().ToString();
            }

            logger.LogInformation($"Inserting item into Cosmos DB: {movieRequest.Id}");
            ItemResponse<MovieRequest> response = await container.CreateItemAsync<MovieRequest>(movieRequest, new PartitionKey(movieRequest.Id));
            logger.LogInformation($"Created item in database with id: {response.Resource.Id}");

            return new OkObjectResult(response.Resource);
        }
        catch (CosmosException cosmosEx)
        {
            logger.LogError(cosmosEx, $"Cosmos DB Error: {cosmosEx.Message}");
            return new ObjectResult(new
            {
                Error = "Failed to save to Cosmos DB",
                Details = cosmosEx.Message
            })
            {
                StatusCode = (int)cosmosEx.StatusCode
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error");
            return new ObjectResult(new
            {
                Error = "Internal server error",
                Details = ex.Message
            })
            {
                StatusCode = 500
            };
        }
    }
}
