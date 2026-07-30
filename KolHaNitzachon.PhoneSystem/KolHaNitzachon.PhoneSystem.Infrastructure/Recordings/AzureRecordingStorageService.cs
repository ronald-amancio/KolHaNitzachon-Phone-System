using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Recordings;

public sealed class AzureRecordingStorageService : IRecordingStorage
{
    private const long MaximumFileSizeBytes = 20_000_000;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3"
        };

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "audio/mpeg",
            "audio/mp3",
            "application/octet-stream"
        };

    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureRecordingStorageService> _logger;

    public AzureRecordingStorageService(
        IConfiguration configuration,
        ILogger<AzureRecordingStorageService> logger)
    {
        var connectionString =
            configuration.GetConnectionString(
                "AzureBlobStorage");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Azure Blob Storage connection string is missing.");
        }

        var containerName =
            configuration["AzureBlobStorage:ContainerName"];

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException(
                "Azure Blob Storage container name is missing.");
        }

        var blobServiceClient =
            new BlobServiceClient(connectionString);

        _containerClient =
            blobServiceClient.GetBlobContainerClient(
                containerName);

        _logger = logger;
    }

    public async Task<RecordingUploadResult> UploadAsync(
        RecordingUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);

        ValidateRequest(request);

        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var extension = Path
            .GetExtension(request.OriginalFileName)
            .ToLowerInvariant();

        var baseFileName =
            Path.GetFileNameWithoutExtension(
                request.OriginalFileName);

        var safeFileName =
            SanitizeFileName(baseFileName);

        var uniqueFileName =
            $"recordings/{safeFileName}-{Guid.NewGuid():N}{extension}";

        var blobClient =
            _containerClient.GetBlobClient(
                uniqueFileName);

        var uploadOptions =
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "audio/mpeg",
                    CacheControl = "private, max-age=3600"
                }
            };

        try
        {
            await blobClient.UploadAsync(
                request.Content,
                uploadOptions,
                cancellationToken);

            _logger.LogInformation(
                "Recording {OriginalFileName} uploaded to Azure as {BlobName}",
                request.OriginalFileName,
                uniqueFileName);

            return new RecordingUploadResult(
                FileName: uniqueFileName,
                RelativeUrl: GenerateSasUrl(uniqueFileName));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to upload recording {OriginalFileName} to Azure Blob Storage",
                request.OriginalFileName);

            throw new RecordingStorageException(
                "The recording could not be uploaded to Azure Blob Storage.",
                exception);
        }
    }

    public async Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var blobName =
            NormalizeBlobName(fileName);

        try
        {
            var blobClient =
                _containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Azure recording {BlobName} deleted",
                blobName);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to delete Azure recording {BlobName}",
                blobName);

            throw new RecordingStorageException(
                "The Azure recording could not be deleted.",
                exception);
        }
    }

    public string GetPlaybackUrl(string fileName, string applicationBaseUrl)
    {
        return GenerateSasUrl(
            NormalizeBlobName(fileName));
    }

    private string GenerateSasUrl(string blobName)
    {
        var blobClient =
            _containerClient.GetBlobClient(blobName);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                "Azure Storage credentials cannot generate SAS URLs.");
        }

        var sasBuilder =
            new BlobSasBuilder
            {
                BlobContainerName =
                    _containerClient.Name,

                BlobName = blobName,
                Resource = "b",
                StartsOn =
                    DateTimeOffset.UtcNow.AddMinutes(-5),

                ExpiresOn =
                    DateTimeOffset.UtcNow.AddHours(4)
            };

        sasBuilder.SetPermissions(
            BlobSasPermissions.Read);

        return blobClient
            .GenerateSasUri(sasBuilder)
            .ToString();
    }

    private static void ValidateRequest(RecordingUploadRequest request)
    {
        if (request.Length <= 0)
        {
            throw new RecordingStorageException(
                "The recording file is empty.");
        }

        if (request.Length > MaximumFileSizeBytes)
        {
            throw new RecordingStorageException(
                "The recording exceeds the maximum allowed size of 20 MB.");
        }

        if (!request.Content.CanRead)
        {
            throw new RecordingStorageException(
                "The recording stream cannot be read.");
        }

        var extension =
            Path.GetExtension(
                request.OriginalFileName);

        if (!AllowedExtensions.Contains(extension))
        {
            throw new RecordingStorageException(
                "Only MP3 recording files are supported.");
        }

        if (!string.IsNullOrWhiteSpace(
                request.ContentType) &&
            !AllowedContentTypes.Contains(
                request.ContentType))
        {
            throw new RecordingStorageException(
                "The selected file is not a supported MP3 recording.");
        }
    }

    private static string NormalizeBlobName(string fileName)
    {
        var normalized =
            fileName
                .Replace('\\', '/')
                .TrimStart('/');

        if (normalized.Contains("..",
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Invalid recording filename.",
                nameof(fileName));
        }

        return normalized;
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "recording";
        }

        var sanitized =
            new string(
                fileName
                    .Trim()
                    .Select(character =>
                        char.IsLetterOrDigit(character) ||
                        character is '-' or '_'
                            ? character
                            : '-')
                    .ToArray())
            .Trim('-', '_');

        return string.IsNullOrWhiteSpace(sanitized)
            ? "recording"
            : sanitized[..Math.Min(
                sanitized.Length,
                80)];
    }
}