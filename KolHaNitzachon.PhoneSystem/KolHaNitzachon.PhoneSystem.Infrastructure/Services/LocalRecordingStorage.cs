using KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Services
{
    public class LocalRecordingStorage : IRecordingStorage
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocalRecordingStorage(
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor)
        {
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> UploadAsync(
            Stream stream,
            string fileName,
            string contentType)
        {
            var folder = Path.Combine(
                _environment.WebRootPath,
                "recordings");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var extension = Path.GetExtension(fileName);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

            var fullPath = Path.Combine(folder, uniqueFileName);

            await using (var fileStream = new FileStream(fullPath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream);
            }

            var request = _httpContextAccessor.HttpContext!.Request;

            return $"{request.Scheme}://{request.Host}/recordings/{uniqueFileName}";
        }

        public Task DeleteAsync(string fileName)
        {
            var fullPath = Path.Combine(
                _environment.WebRootPath,
                "recordings",
                fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }
    }
}
