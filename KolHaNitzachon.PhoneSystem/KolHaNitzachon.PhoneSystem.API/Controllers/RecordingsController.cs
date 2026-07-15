using KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KolHaNitzachon.PhoneSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecordingsController : ControllerBase
    {
        private readonly IRecordingStorage _storage;

        public RecordingsController(IRecordingStorage storage)
        {
            _storage = storage;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null)
                return BadRequest();

            await using var stream = file.OpenReadStream();

            var url = await _storage.UploadAsync(
                stream,
                file.FileName,
                file.ContentType);

            return Ok(new
            {
                url
            });
        }
    }
}