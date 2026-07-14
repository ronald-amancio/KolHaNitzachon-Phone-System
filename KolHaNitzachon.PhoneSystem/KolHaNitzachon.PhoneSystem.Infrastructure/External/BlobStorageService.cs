using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.External;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.External;

public class BlobStorageService : IBlobStorageService
{
    private readonly string _connectionString;
    private readonly string _containerName = "feedback-recordings";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<BlobStorageService> logger)
    {
        _connectionString = configuration.GetConnectionString("AzureBlobStorage")
            ?? throw new ArgumentNullException("AzureBlobStorage connection string is missing in appsettings.json");
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
                HttpHeaders = new BlobHttpHeaders { ContentType = "audio/wav" }
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
    public string GenerateSasUrl(string blobName)
    {
        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!blobClient.CanGenerateSasUri)
        {
            _logger.LogError("Storage account key is required to generate SAS for: {BlobName}", blobName);
            throw new InvalidOperationException("Account key not configured for SAS generation.");
        }

        // Define SAS permissions and expiry (1 hour is usually sufficient for playback)
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b", // 'b' stands for blob
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
        return sasUri.ToString();
    }
}
