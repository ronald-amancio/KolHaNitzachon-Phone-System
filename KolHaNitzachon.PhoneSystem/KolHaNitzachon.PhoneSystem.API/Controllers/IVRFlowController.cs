using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Repositories;
using KolHaNitzachon.PhoneSystem.Application.Models;
using KolHaNitzachon.PhoneSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Twilio.TwiML;

namespace KolHaNitzachon.PhoneSystem.API.Controllers
{
    [ApiController]
    [Route("api/ivr")]
    [Produces("application/xml")]
    public sealed class IVRFlowController : ControllerBase
    {
        private const decimal MinimumDonationAmount = 1m;
        private const decimal MaximumTestDonationAmount = 1000m;

        private readonly IMenuRenderer _menuRenderer;
        private readonly ILogger<IVRFlowController> _logger;
        private readonly IIvrCallSessionStore _sessionStore;

        public IVRFlowController(
            IMenuRenderer menuRenderer,
            IIvrCallSessionStore sessionStore,
            ILogger<IVRFlowController> logger)
        {
            _menuRenderer = menuRenderer;
            _sessionStore = sessionStore;
            _logger = logger;
        }

        [HttpPost("handle-call")]
        [Consumes(
    "application/x-www-form-urlencoded",
    "multipart/form-data")]
        public IActionResult HandleCall(
    [FromQuery] string? step,
    [FromForm(Name = "CallSid")] string? callSid,
    [FromForm(Name = "From")] string? from,
    [FromForm(Name = "Digits")] string? digits)
        {
            try
            {
                step = NormalizeStep(step);
                digits = NormalizeDigits(digits);

                var effectiveCallSid =
                    ResolveCallSid(callSid);

                var session = _sessionStore.GetOrCreate(
                    effectiveCallSid,
                    from);

                session.CurrentStep = step;

                _sessionStore.Update(session);

                var applicationBaseUrl =
                    GetApplicationBaseUrl();

                var recordingBaseUrl =
                    $"{applicationBaseUrl}/audio";

                _logger.LogInformation(
                    "IVR request. Step={Step}, CallSid={CallSid}, " +
                    "From={From}, Digits={Digits}, " +
                    "DonationType={DonationType}, " +
                    "DonationAmount={DonationAmount}",
                    step,
                    effectiveCallSid,
                    from ?? "none",
                    digits ?? "none",
                    session.DonationType,
                    session.DonationAmount);

                return step switch
                {
                    "main" => HandleMainMenu(
                        session,
                        digits,
                        applicationBaseUrl,
                        recordingBaseUrl),

                    "sponsor-all" => HandleSponsorAll(
                        session,
                        digits,
                        applicationBaseUrl,
                        recordingBaseUrl),

                    "donation-amount" => HandleDonationAmount(
                        session,
                        digits,
                        applicationBaseUrl,
                        recordingBaseUrl),

                    "confirm-donation" => HandleDonationConfirmation(
                        session,
                        digits,
                        applicationBaseUrl,
                        recordingBaseUrl),

                    "end-call" => EndCall(
                        session,
                        recordingBaseUrl),

                    _ => Xml(
                        _menuRenderer.RenderInvalidOption(
                            BuildActionUrl(
                                applicationBaseUrl,
                                "main")))
                };
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while processing the IVR call.");

                var response = new VoiceResponse();

                response.Say(
                    "A technical error occurred. Please try again later.");

                response.Hangup();

                return Xml(response);
            }
        }

        [HttpGet("debug/session/{callSid}")]
        [Produces("application/json")]
        public IActionResult GetSession(string callSid)
        {
            if (!HttpContext.RequestServices
                    .GetRequiredService<IHostEnvironment>()
                    .IsDevelopment())
            {
                return NotFound();
            }

            if (!_sessionStore.TryGet(
                    callSid,
                    out var session))
            {
                return NotFound(
                    new
                    {
                        message = "IVR session was not found."
                    });
            }

            return Ok(
                new
                {
                    session!.CallSid,
                    session.CallerPhoneNumber,
                    DonationType =
                        session.DonationType?.ToString(),
                    session.DonationAmount,
                    session.RecipientId,
                    session.RecipientCode,
                    session.CurrentStep,
                    session.CreatedAtUtc,
                    session.LastUpdatedAtUtc,
                    session.ExpiresAtUtc
                });
        }

        #region Helpers
        private static string ResolveCallSid(string? callSid)
        {
            if (!string.IsNullOrWhiteSpace(callSid))
            {
                return callSid.Trim();
            }

            // Only a fallback for Swagger or local manual testing.
            return $"LOCAL-{Guid.NewGuid():N}";
        }

        private IActionResult HandleDonationConfirmation(IvrCallSession session, string? digits, string applicationBaseUrl, string recordingBaseUrl)
        {
            if (session.DonationAmount is null)
            {
                _logger.LogWarning(
                    "Donation confirmation requested without " +
                    "an amount. CallSid={CallSid}",
                    session.CallSid);

                session.CurrentStep = "donation-amount";

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderEnterDonationAmount(
                        BuildActionUrl(
                            applicationBaseUrl,
                            "donation-amount"),
                        recordingBaseUrl));
            }

            var confirmationActionUrl = BuildActionUrl(
                applicationBaseUrl,
                "confirm-donation");

            if (string.IsNullOrWhiteSpace(digits))
            {
                return Xml(
                    _menuRenderer.RenderDonationConfirmation(
                        session.DonationAmount.Value,
                        confirmationActionUrl,
                        recordingBaseUrl));
            }

            switch (digits)
            {
                case "1":
                    session.CurrentStep = "payment";
                    _sessionStore.Update(session);

                    return Xml(
                        _menuRenderer.RenderPreparingPayment(
                            recordingBaseUrl));

                case "2":
                    session.DonationAmount = null;
                    session.CurrentStep = "donation-amount";

                    _sessionStore.Update(session);

                    return Xml(
                        _menuRenderer.RenderEnterDonationAmount(
                            BuildActionUrl(
                                applicationBaseUrl,
                                "donation-amount"),
                            recordingBaseUrl));

                case "9":
                    ResetSessionForMainMenu(session);

                    return Xml(
                        _menuRenderer.RenderMainMenu(
                            BuildActionUrl(
                                applicationBaseUrl,
                                "main"),
                            recordingBaseUrl));

                default:
                    return Xml(
                        _menuRenderer.RenderInvalidOption(
                            confirmationActionUrl));
            }
        }

        private void ResetSessionForMainMenu(IvrCallSession session)
        {
            session.CurrentStep = "main";
            session.DonationType = null;
            session.DonationAmount = null;
            session.RecipientId = null;
            session.RecipientCode = null;

            _sessionStore.Update(session);
        }

        private IActionResult EndCall(IvrCallSession session, string recordingBaseUrl)
        {
            _sessionStore.Remove(session.CallSid);

            var response = new VoiceResponse();

            response.Say("Thank you for calling. Goodbye.");

            response.Hangup();

            return Xml(response);
        }
        #endregion

        [HttpPost("test-payment-success")]
        public IActionResult TestPaymentSuccess()
        {
            var recordingBaseUrl =
                $"{GetApplicationBaseUrl()}/audio";

            return Xml(
                _menuRenderer.RenderPaymentSuccessful(
                    recordingBaseUrl));
        }

        [HttpPost("test-payment-failure")]
        public IActionResult TestPaymentFailure()
        {
            var applicationBaseUrl = GetApplicationBaseUrl();
            var recordingBaseUrl =
                $"{applicationBaseUrl}/audio";

            return Xml(
                _menuRenderer.RenderPaymentFailed(
                    BuildActionUrl(
                        applicationBaseUrl,
                        "main"),
                    recordingBaseUrl));
        }

        private IActionResult HandleMainMenu(
    IvrCallSession session,
    string? digits,
    string applicationBaseUrl,
    string recordingBaseUrl)
        {
            var actionUrl = BuildActionUrl(
                applicationBaseUrl,
                "main");

            if (string.IsNullOrWhiteSpace(digits))
            {
                session.CurrentStep = "main";

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderMainMenu(
                        actionUrl,
                        recordingBaseUrl));
            }

            return digits switch
            {
                "1" => StartSponsorAllFlow(
                    session,
                    applicationBaseUrl,
                    recordingBaseUrl),

                _ => Xml(
                    _menuRenderer.RenderInvalidOption(
                        actionUrl))
            };
        }

        private IActionResult StartSponsorAllFlow(IvrCallSession session, string applicationBaseUrl, string recordingBaseUrl)
        {
            session.CurrentStep = "sponsor-all";

            // Clear values left over if the caller restarted the menu.
            session.DonationType = null;
            session.DonationAmount = null;
            session.RecipientId = null;
            session.RecipientCode = null;

            _sessionStore.Update(session);

            return Xml(
                _menuRenderer.RenderSponsorAllMenu(
                    BuildActionUrl(
                        applicationBaseUrl,
                        "sponsor-all"),
                    recordingBaseUrl));
        }

        private IActionResult HandleSponsorAll(IvrCallSession session, string? digits, string applicationBaseUrl, string recordingBaseUrl)
        {
            var currentActionUrl = BuildActionUrl(
                applicationBaseUrl,
                "sponsor-all");

            if (string.IsNullOrWhiteSpace(digits))
            {
                session.CurrentStep = "sponsor-all";

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderSponsorAllMenu(
                        currentActionUrl,
                        recordingBaseUrl));
            }

            switch (digits)
            {
                case "1":
                    session.DonationType =
                        DonationType.DonateToAllPerDay;

                    break;

                case "2":
                    session.DonationType =
                        DonationType.DonateToAllSameAmount;

                    break;

                default:
                    return Xml(
                        _menuRenderer.RenderInvalidOption(
                            currentActionUrl));
            }

            session.CurrentStep = "donation-amount";
            session.DonationAmount = null;

            _sessionStore.Update(session);

            return Xml(
                _menuRenderer.RenderEnterDonationAmount(
                    BuildActionUrl(
                        applicationBaseUrl,
                        "donation-amount"),
                    recordingBaseUrl));
        }

        private IActionResult HandleDonationAmount(IvrCallSession session, string? digits, string applicationBaseUrl, string recordingBaseUrl)
        {
            var donationActionUrl = BuildActionUrl(
                applicationBaseUrl,
                "donation-amount");

            if (session.DonationType is null or
                DonationType.Unknown)
            {
                _logger.LogWarning(
                    "Donation amount requested without a donation type. " +
                    "CallSid={CallSid}",
                    session.CallSid);

                session.CurrentStep = "sponsor-all";

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderSponsorAllMenu(
                        BuildActionUrl(
                            applicationBaseUrl,
                            "sponsor-all"),
                        recordingBaseUrl));
            }

            if (string.IsNullOrWhiteSpace(digits))
            {
                session.CurrentStep = "donation-amount";

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderEnterDonationAmount(
                        donationActionUrl,
                        recordingBaseUrl));
            }

            if (!TryParseDonationAmount(
                    digits,
                    out var donationAmount))
            {
                return Xml(
                    _menuRenderer.RenderInvalidDonationAmount(
                        donationActionUrl,
                        recordingBaseUrl));
            }

            if (donationAmount < MinimumDonationAmount ||
                donationAmount > MaximumTestDonationAmount)
            {
                return Xml(
                    _menuRenderer.RenderInvalidDonationAmount(
                        donationActionUrl,
                        recordingBaseUrl));
            }

            session.DonationAmount = donationAmount;
            session.CurrentStep = "confirm-donation";

            _sessionStore.Update(session);

            return Xml(
                _menuRenderer.RenderDonationConfirmation(
                    donationAmount,
                    BuildActionUrl(
                        applicationBaseUrl,
                        "confirm-donation"),
                    recordingBaseUrl));
        }

        private string GetApplicationBaseUrl()
        {
            // X-Forwarded headers are important when running through
            // Cloudflare Tunnel, ngrok, Render, Azure, or another proxy.
            var forwardedProtocol =
                Request.Headers["X-Forwarded-Proto"]
                    .FirstOrDefault();

            var forwardedHost =
                Request.Headers["X-Forwarded-Host"]
                    .FirstOrDefault();

            var scheme = string.IsNullOrWhiteSpace(
                forwardedProtocol)
                ? Request.Scheme
                : forwardedProtocol;

            var host = string.IsNullOrWhiteSpace(
                forwardedHost)
                ? Request.Host.Value
                : forwardedHost;

            return $"{scheme}://{host}".TrimEnd('/');
        }

        private static bool TryParseDonationAmount(
            string digits,
            out decimal amount)
        {
            amount = 0;

            var cleanedDigits = new string(
                digits
                    .Where(char.IsDigit)
                    .ToArray());

            if (string.IsNullOrWhiteSpace(cleanedDigits))
            {
                return false;
            }

            return decimal.TryParse(
                cleanedDigits,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out amount);
        }

        private static string BuildActionUrl(
            string applicationBaseUrl,
            string step)
        {
            return
                $"{applicationBaseUrl.TrimEnd('/')}" +
                $"/api/ivr/handle-call" +
                $"?step={Uri.EscapeDataString(step)}";
        }

        private static string NormalizeStep(string? step)
        {
            return string.IsNullOrWhiteSpace(step)
                ? "main"
                : step.Trim().ToLowerInvariant();
        }

        private static string? NormalizeDigits(string? digits)
        {
            return string.IsNullOrWhiteSpace(digits)
                ? null
                : digits.Trim();
        }

        private ContentResult Xml(VoiceResponse response)
        {
            return Content(
                response.ToString(),
                "application/xml");
        }
    }
}