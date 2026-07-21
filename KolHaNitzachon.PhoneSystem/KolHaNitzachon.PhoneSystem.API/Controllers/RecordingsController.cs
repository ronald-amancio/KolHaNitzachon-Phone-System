using KolHaNitzachon.PhoneSystem.API.Contracts.Recordings;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings;
using Microsoft.AspNetCore.Mvc;

namespace KolHaNitzachon.PhoneSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecordingsController : ControllerBase
    {
        private const long MaximumRequestSizeBytes = 20_000_000;

        private readonly IRecordingStorage _recordingStorage;
        private readonly ILogger<RecordingsController> _logger;

        public RecordingsController(
            IRecordingStorage recordingStorage,
            ILogger<RecordingsController> logger)
        {
            _recordingStorage = recordingStorage;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType<RecordingUploadResponse>(
            StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(
            StatusCodes.Status500InternalServerError)]
        [RequestSizeLimit(MaximumRequestSizeBytes)]
        public async Task<ActionResult<RecordingUploadResponse>> Upload(
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Recording file required",
                    "No recording file was provided."));
            }

            await using var stream = file.OpenReadStream();

            var request = new RecordingUploadRequest(
                Content: stream,
                OriginalFileName: file.FileName,
                ContentType: file.ContentType,
                Length: file.Length);

            try
            {
                var result = await _recordingStorage.UploadAsync(
                    request,
                    cancellationToken);

                var absoluteUrl = BuildAbsoluteUrl(result.RelativeUrl);

                return Ok(new RecordingUploadResponse(
                    FileName: result.FileName,
                    Url: absoluteUrl));
            }
            catch (RecordingStorageException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Recording upload rejected for file {FileName}",
                    file.FileName);

                return BadRequest(CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Recording upload failed",
                    exception.Message));
            }
        }

        private string BuildAbsoluteUrl(string relativeUrl)
        {
            var normalizedUrl = relativeUrl.StartsWith('/')
                ? relativeUrl
                : $"/{relativeUrl}";

            return $"{Request.Scheme}://{Request.Host}" +
                   $"{Request.PathBase}{normalizedUrl}";
        }

        private static ProblemDetails CreateProblemDetails(
            int status,
            string title,
            string detail)
        {
            return new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            };
        }
    }
}