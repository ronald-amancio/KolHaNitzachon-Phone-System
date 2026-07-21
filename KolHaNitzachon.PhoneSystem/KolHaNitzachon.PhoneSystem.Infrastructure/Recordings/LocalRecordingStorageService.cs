using KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings;
using KolHaNitzachon.PhoneSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Recordings
{
    public sealed class LocalRecordingStorageService : IRecordingStorage
    {
        private const long MaximumFileSizeBytes = 20_000_000;

        private static readonly string[] AllowedExtensions =
        [
            ".mp3"
        ];

        private static readonly string[] AllowedContentTypes =
        [
            "audio/mpeg",
            "audio/mp3",
            "application/octet-stream"
        ];

        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LocalRecordingStorageService> _logger;

        public LocalRecordingStorageService(
            IWebHostEnvironment environment,
            ILogger<LocalRecordingStorageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<RecordingUploadResult> UploadAsync(
            RecordingUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Content);

            ValidateRequest(request);

            var recordingsFolder = GetRecordingsFolder();

            Directory.CreateDirectory(recordingsFolder);

            var extension = Path
                .GetExtension(request.OriginalFileName)
                .ToLowerInvariant();

            var baseFileName = Path.GetFileNameWithoutExtension(
                request.OriginalFileName);

            var safeFileName = SanitizeFileName(baseFileName);

            var uniqueFileName =
                $"{safeFileName}-{Guid.NewGuid():N}{extension}";

            var fullPath = Path.Combine(
                recordingsFolder,
                uniqueFileName);

            try
            {
                await using var fileStream = new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                await request.Content.CopyToAsync(
                    fileStream,
                    cancellationToken);

                var relativeUrl =
                    $"/recordings/{Uri.EscapeDataString(uniqueFileName)}";

                _logger.LogInformation(
                    "Recording {OriginalFileName} saved as {StoredFileName}",
                    request.OriginalFileName,
                    uniqueFileName);

                return new RecordingUploadResult(
                    FileName: uniqueFileName,
                    RelativeUrl: relativeUrl);
            }
            catch (OperationCanceledException)
            {
                DeleteIncompleteFile(fullPath);
                throw;
            }
            catch (Exception exception)
            {
                DeleteIncompleteFile(fullPath);

                _logger.LogError(
                    exception,
                    "Failed to save recording {OriginalFileName}",
                    request.OriginalFileName);

                throw new RecordingStorageException(
                    "The recording could not be saved.",
                    exception);
            }
        }

        public Task DeleteAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Task.CompletedTask;
            }

            var safeFileName = Path.GetFileName(fileName);

            var fullPath = Path.Combine(
                GetRecordingsFolder(),
                safeFileName);

            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);

                    _logger.LogInformation(
                        "Recording {FileName} deleted",
                        safeFileName);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to delete recording {FileName}",
                    safeFileName);

                throw new RecordingStorageException(
                    "The recording could not be deleted.",
                    exception);
            }

            return Task.CompletedTask;
        }

        private void ValidateRequest(RecordingUploadRequest request)
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

            var extension = Path
                .GetExtension(request.OriginalFileName)
                .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extension) ||
                !AllowedExtensions.Contains(
                    extension,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new RecordingStorageException(
                    "Only MP3 recording files are supported.");
            }

            if (!string.IsNullOrWhiteSpace(request.ContentType) &&
                !AllowedContentTypes.Contains(
                    request.ContentType,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new RecordingStorageException(
                    "The selected file is not a supported MP3 recording.");
            }
        }

        private string GetRecordingsFolder()
        {
            var webRootPath = string.IsNullOrWhiteSpace(
                _environment.WebRootPath)
                ? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot")
                : _environment.WebRootPath;

            return Path.Combine(
                webRootPath,
                "recordings");
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "recording";
            }

            var sanitized = new string(
                fileName
                    .Trim()
                    .Select(character =>
                        char.IsLetterOrDigit(character) ||
                        character is '-' or '_'
                            ? character
                            : '-')
                    .ToArray());

            sanitized = sanitized
                .Trim('-', '_');

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return "recording";
            }

            const int maximumLength = 80;

            return sanitized.Length <= maximumLength
                ? sanitized
                : sanitized[..maximumLength];
        }

        private static void DeleteIncompleteFile(string fullPath)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch
            {
                // Preserve the original upload exception.
            }
        }
    }
}