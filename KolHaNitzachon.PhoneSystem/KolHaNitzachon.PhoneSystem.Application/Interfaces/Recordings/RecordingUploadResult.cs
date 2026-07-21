using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings
{
    public sealed record RecordingUploadResult(string FileName, string RelativeUrl);
}