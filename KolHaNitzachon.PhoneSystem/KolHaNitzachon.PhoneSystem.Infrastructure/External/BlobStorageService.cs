using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.External;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Storage.Sas;

namespace Infrastructure.External;

public class BlobStorageService : IBlobStorageService
{
    private readonly string _connectionString;
    //private readonly string _containerName = "feedback-recordings";
    private readonly string _containerName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<BlobStorageService> logger)
    {
        _connectionString =
            configuration.GetConnectionString(
                "AzureBlobStorage")
            ?? throw new InvalidOperationException(
                "Azure Blob Storage connection string is missing.");

        _containerName =
            configuration[
                "AzureBlobStorage:ContainerName"]
            ?? throw new InvalidOperationException(
                "Azure Blob Storage container name is missing.");

        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Downloads recording from SignalWire and uploads it to our private Azure container.
    /// </summary>
    public async Task<string> UploadRecordingAsync(string signalWireUrl, string fileName)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            // Ensures container is created with private access (SAS required)
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Fetch stream from SignalWire
            using var client = _httpClientFactory.CreateClient();
            using var audioStream = await client.GetStreamAsync(signalWireUrl);

            var blobClient = containerClient.GetBlobClient(fileName);

            // Set headers so SignalWire recognizes it as audio during playback
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "audio/mpeg" //"audio/wav" 
                }
            }; 

            await blobClient.UploadAsync(audioStream, uploadOptions);

            _logger.LogInformation("Successfully stored feedback: {FileName}", fileName);
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate recording from SignalWire to Azure: {Url}", signalWireUrl);
            throw;
        }
    }

    /// <summary>
    /// Lists all recording filenames in the container, sorted by Created Date (Newest First).
    /// </summary>
    public async Task<List<string>> ListRecordingsDescendingAsync()
    {
        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

        var blobs = new List<BlobItem>();

        // Async enumeration of all blobs in the container
        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            blobs.Add(blobItem);
        }

        // Return sorted list by creation date descending
        return blobs
            .OrderByDescending(b => b.Properties.CreatedOn)
            .Select(b => b.Name)
            .ToList();
    }

    /// <summary>
    /// Generates a time-limited SAS URL that grants SignalWire temporary READ access.
    /// </summary>
    #region NotInUsed
    //public string GenerateSasUrl(string blobName)
    //{
    //    var blobServiceClient = new BlobServiceClient(_connectionString);
    //    var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
    //    var blobClient = containerClient.GetBlobClient(blobName);

    //    if (!blobClient.CanGenerateSasUri)
    //    {
    //        _logger.LogError("Storage account key is required to generate SAS for: {BlobName}", blobName);
    //        throw new InvalidOperationException("Account key not configured for SAS generation.");
    //    }

    //    // Define SAS permissions and expiry (1 hour is usually sufficient for playback)
    //    var sasBuilder = new BlobSasBuilder
    //    {
    //        BlobContainerName = _containerName,
    //        BlobName = blobName,
    //        Resource = "b", // 'b' stands for blob
    //        ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
    //    };

    //    sasBuilder.SetPermissions(BlobSasPermissions.Read);

    //    Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
    //    return sasUri.ToString();
    //}
    #endregion

    #region GenerateSasURLBak
    //public string GenerateSasUrl(string blobName)
    //{
    //    if (string.IsNullOrWhiteSpace(blobName))
    //    {
    //        throw new ArgumentException(
    //            "Blob name is required.",
    //            nameof(blobName));
    //    }

    //    var blobServiceClient =
    //        new BlobServiceClient(
    //            _connectionString);

    //    var containerClient =
    //        blobServiceClient.GetBlobContainerClient(
    //            _containerName);

    //    var blobClient =
    //        containerClient.GetBlobClient(
    //            blobName);

    //    return blobClient.Uri.ToString();
    //}
    #endregion

    //public string GenerateSasUrl(string blobName)
    //{
    //    if (string.IsNullOrWhiteSpace(blobName))
    //    {
    //        throw new ArgumentException(
    //            "Blob name is required.",
    //            nameof(blobName));
    //    }

    //    var blobServiceClient =
    //        new BlobServiceClient(_connectionString);

    //    var containerClient =
    //        blobServiceClient.GetBlobContainerClient(
    //            _containerName);

    //    var blobClient =
    //        containerClient.GetBlobClient(blobName);

    //    if (!blobClient.CanGenerateSasUri)
    //    {
    //        _logger.LogError(
    //            "Unable to generate SAS URL for blob {BlobName}. " +
    //            "The configured connection string may not contain an account key.",
    //            blobName);

    //        throw new InvalidOperationException(
    //            "The configured Azure Storage credentials cannot generate SAS URLs.");
    //    }

    //    var sasBuilder = new BlobSasBuilder
    //    {
    //        BlobContainerName = _containerName,
    //        BlobName = blobName,
    //        Resource = "b",
    //        StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
    //        ExpiresOn = DateTimeOffset.UtcNow.AddHours(4)
    //    };

    //    sasBuilder.SetPermissions(
    //        BlobSasPermissions.Read);

    //    return blobClient
    //        .GenerateSasUri(sasBuilder)
    //        .ToString();
    //}

    public string GenerateSasUrl(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException(
                "Blob name is required.",
                nameof(blobName));
        }

        var blobServiceClient =
            new BlobServiceClient(_connectionString);

        var containerClient =
            blobServiceClient.GetBlobContainerClient(
                _containerName);

        var blobClient =
            containerClient.GetBlobClient(blobName);

        // Account-key connection string:
        // generate a new short-lived SAS.
        if (blobClient.CanGenerateSasUri)
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(4)
            };

            sasBuilder.SetPermissions(
                BlobSasPermissions.Read);

            return blobClient
                .GenerateSasUri(sasBuilder)
                .ToString();
        }

        // Existing SAS connection string:
        // BlobClient.Uri already carries the configured SAS credential.
        var existingSasUrl =
            blobClient.Uri.ToString();

        if (!existingSasUrl.Contains(
                "sig=",
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "The Azure Blob client has neither an account key nor an existing SAS credential.");

            throw new InvalidOperationException(
                "Azure Blob credentials cannot provide a playback URL.");
        }

        return existingSasUrl;
    }
}