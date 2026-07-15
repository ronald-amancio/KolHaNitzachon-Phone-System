using KolHaNitzachon.PhoneSystem.Application.Interfaces.Voice;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KolHaNitzachon.PhoneSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoiceController : ControllerBase
    {
        private readonly IVoiceService _voiceService;

        public VoiceController(IVoiceService voiceService)
        {
            _voiceService = voiceService;
        }

        [HttpPost("call")]
        public async Task<IActionResult> Call(
            string phoneNumber,
            string recordingUrl)
        {
            var sid = await _voiceService.CallAsync(
                phoneNumber,
                recordingUrl);

            return Ok(new
            {
                CallSid = sid
            });
        }
    }
}