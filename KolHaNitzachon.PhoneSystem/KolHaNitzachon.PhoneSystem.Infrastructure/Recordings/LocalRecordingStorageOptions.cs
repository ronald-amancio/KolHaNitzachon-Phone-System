using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Recordings
{
    public sealed class LocalRecordingStorageOptions
    {
        public const string SectionName = "RecordingStorage";

        public string FolderName { get; init; } = "recordings";

        public long MaximumFileSizeBytes { get; init; } = 20_000_000;

        public string[] AllowedExtensions { get; init; } =
        [
            ".mp3"
        ];

        public string[] AllowedContentTypes { get; init; } =
        [
            "audio/mpeg",
            "audio/mp3",
            "application/octet-stream"
        ];
    }
}