using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings
{
    public interface IRecordingStorage
    {
        Task<RecordingUploadResult> UploadAsync(RecordingUploadRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
        string GetPlaybackUrl(string fileName, string applicationBaseUrl);
    }
}