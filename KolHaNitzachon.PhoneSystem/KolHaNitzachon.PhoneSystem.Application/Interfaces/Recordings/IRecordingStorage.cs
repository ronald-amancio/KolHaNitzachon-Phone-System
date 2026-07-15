using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings
{
    public interface IRecordingStorage
    {
        Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType);

        Task DeleteAsync(string fileName);
    }
}
