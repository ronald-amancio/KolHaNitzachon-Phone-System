using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KolHaNitzachon.PhoneSystem.API.Controllers
{
    [ApiController]
    [Route("api/ivr")]
    public class IVRFlowController : ControllerBase
    {
        private readonly IMenuRenderer _menuRenderer;

        public IVRFlowController(IMenuRenderer menuRenderer)
        {
            _menuRenderer = menuRenderer;
        }

        [HttpPost("handle-call")]
        public IActionResult HandleCall(
            [FromForm] string? Digits)
        {
            var response = _menuRenderer.RenderMainMenu(Digits);

            return Content(response.ToString(),"text/xml");
        }
    }
}