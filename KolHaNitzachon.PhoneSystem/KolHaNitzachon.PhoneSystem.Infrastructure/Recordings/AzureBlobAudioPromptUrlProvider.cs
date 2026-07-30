using KolHaNitzachon.PhoneSystem.Application.Interfaces.External;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Recordings
{
    public sealed class AzureBlobAudioPromptUrlProvider : IAudioPromptUrlProvider
    {
        private readonly IBlobStorageService _blobStorageService;

        public AzureBlobAudioPromptUrlProvider(IBlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        public string GetPromptUrl(string relativePath, string applicationBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException(
                    "Audio prompt path is required.",
                    nameof(relativePath));
            }

            var normalizedPath = relativePath
                .Replace('\\', '/')
                .TrimStart('/');

            var blobName = $"audio/{normalizedPath}";

            return _blobStorageService.GenerateSasUrl(blobName);
        }
    }
}
