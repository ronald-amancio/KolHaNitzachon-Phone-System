using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Voice;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KolHaNitzachon.PhoneSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoiceController : ControllerBase
    {
        private readonly IMenuRenderer _menuRenderer;

        public VoiceController(
            IMenuRenderer menuRenderer)
        {
            _menuRenderer = menuRenderer;
        }

        [HttpPost("handle-call")]
        public IActionResult HandleCall()
        {
            var digits = Request.Form["Digits"].ToString();

            var response = _menuRenderer.RenderMainMenu(digits);

            return Content(
                response.ToString(),
                "application/xml");
        }
    }
}