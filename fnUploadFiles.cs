using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Newtonsoft.Json;
using Azure.Storage.Blobs.Models;
using System.Reflection.Metadata;

namespace fnUploadfile
{
    public static class fnUploadFiles
    {
        [FunctionName("fnUploadFiles")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger logger)
        {
            logger.LogInformation("Uploading files to Azure Storage Account.");

            try
            {
                if (!req.Headers.TryGetValue("fileTypeHeader", out var fileTypeHeader))
                {
                    return new BadRequestObjectResult("Header error: File type not specified.");
                }

                var fileType = fileTypeHeader.ToString();
                var formData = await req.ReadFormAsync();
                var file = formData.Files["file"];

                if (file == null || file.Length == 0)
                {
                    return new BadRequestObjectResult("Please upload a file");
                }

                const long maxFileSize = 50 * 1024 * 1024;
                if (file.Length > maxFileSize)
                {
                    return new BadRequestObjectResult(
                        $"The file is to large, exceeding the max size of {(maxFileSize / 1024) / 1024}MB");
                }


                var fileName = $"{Guid.NewGuid().ToString()}{Path.GetExtension(file.FileName)}";

                var staConnectuionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                string containerName = $"{fileType.ToLower()}s";
                BlobClient blobClient = new(staConnectuionString, containerName, fileName);
                BlobContainerClient containerClient = new(staConnectuionString, containerName);

                await containerClient.CreateIfNotExistsAsync();
                containerClient.SetAccessPolicy(PublicAccessType.BlobContainer);

                string blobName = fileName;
                var blob = containerClient.GetBlobClient(blobName);

                using (var fileStream = file.OpenReadStream())
                {
                    await blob.UploadAsync(fileStream, true);
                }

                blobClient.Upload(file.OpenReadStream(), true);

                logger.LogInformation($"File uploaded to Azure Storage Account: {fileName}");

                return new OkObjectResult(new
                {
                    statusCode = req.HttpContext.Response.StatusCode,
                    message = "File uploaded successfully.",
                    fileUri = blob.Uri
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return new BadRequestObjectResult("Error uploading files to Azure Storage Account.");
            }


        }
    }
}
