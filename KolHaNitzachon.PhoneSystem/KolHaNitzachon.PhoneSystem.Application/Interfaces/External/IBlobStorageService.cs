using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.External
{
    public interface IBlobStorageService
    {
        Task<string> UploadRecordingAsync(string signalWireUrl, string fileName);
        Task<List<string>> ListRecordingsDescendingAsync();
        string GenerateSasUrl(string blobName);
    }
}