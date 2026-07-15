using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KolHaNitzachon.PhoneSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TwiMLController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get(string recordingUrl)
        {
            var xml = $"""
                        <Response>
                            <Play>{recordingUrl}</Play>
                        </Response>
                        """;

            //var url = $"https://YOURDOMAIN/api/twiml?recordingUrl={Uri.EscapeDataString(recordingUrl)}";

            return Content(xml, "text/xml");
        }
    }
}