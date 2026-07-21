using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings
{
    public class RecordingStorageException : Exception
    {
        public RecordingStorageException(string message) : base(message)
        {

        }

        public RecordingStorageException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}