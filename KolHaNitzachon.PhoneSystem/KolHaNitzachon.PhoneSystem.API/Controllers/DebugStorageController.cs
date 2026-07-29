using Azure;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KolHaNitzachon.PhoneSystem.API.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)] //set to hide from swagger
    [ApiController]
    [Route("api/debug/storage")]
    public sealed class DebugStorageController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DebugStorageController> _logger;

        public DebugStorageController(
            IConfiguration configuration,
            ILogger<DebugStorageController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("azure-blob")]
        public async Task<IActionResult> TestAzureBlobStorage(
            CancellationToken cancellationToken)
        {
            try
            {
                var connectionString =
                    _configuration.GetConnectionString(
                        "AzureBlobStorage");

                var containerName =
                    _configuration[
                        "AzureBlobStorage:ContainerName"];

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new
                        {
                            connected = false,
                            message =
                                "Azure Blob Storage connection string is missing."
                        });
                }

                if (string.IsNullOrWhiteSpace(containerName))
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new
                        {
                            connected = false,
                            message =
                                "Azure Blob Storage container name is missing."
                        });
                }

                var blobServiceClient =
                    new BlobServiceClient(connectionString);

                var containerClient =
                    blobServiceClient.GetBlobContainerClient(
                        containerName);

                var exists =
                    await containerClient.ExistsAsync(
                        cancellationToken);

                if (!exists.Value)
                {
                    return NotFound(
                        new
                        {
                            connected = true,
                            container = containerName,
                            exists = false,
                            message =
                                "Azure Storage was reached, but the container was not found."
                        });
                }

                var sampleFiles =
                    new List<string>();

                await foreach (
                    var blob in containerClient
                        .GetBlobsAsync(
                            cancellationToken:
                                cancellationToken))
                {
                    sampleFiles.Add(blob.Name);

                    if (sampleFiles.Count >= 5)
                    {
                        break;
                    }
                }

                return Ok(
                    new
                    {
                        connected = true,
                        container = containerName,
                        exists = true,
                        sampleFileCount =
                            sampleFiles.Count,
                        sampleFiles
                    });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(
                    ex,
                    "Azure Blob Storage test failed. Status={Status}, ErrorCode={ErrorCode}",
                    ex.Status,
                    ex.ErrorCode);

                return StatusCode(
                    ex.Status > 0
                        ? ex.Status
                        : StatusCodes.Status500InternalServerError,
                    new
                    {
                        connected = false,
                        status = ex.Status,
                        errorCode = ex.ErrorCode,
                        message = ex.Message
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected Azure Blob Storage test failure.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        connected = false,
                        message = ex.Message
                    });
            }
        }
    }
}