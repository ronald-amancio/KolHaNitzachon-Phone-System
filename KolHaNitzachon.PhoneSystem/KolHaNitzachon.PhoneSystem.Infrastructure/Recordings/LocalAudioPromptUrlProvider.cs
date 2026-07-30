using KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Recordings
{
    public sealed class LocalAudioPromptUrlProvider : IAudioPromptUrlProvider
    {
        public string GetPromptUrl(string relativePath, string applicationBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException(
                    "Audio prompt path is required.",
                    nameof(relativePath));
            }

            if (string.IsNullOrWhiteSpace(applicationBaseUrl))
            {
                throw new ArgumentException(
                    "Application base URL is required.",
                    nameof(applicationBaseUrl));
            }

            var normalizedPath = relativePath
                .Replace('\\', '/')
                .TrimStart('/');

            return
                $"{applicationBaseUrl.TrimEnd('/')}/audio/{normalizedPath}";
        }
    }
}