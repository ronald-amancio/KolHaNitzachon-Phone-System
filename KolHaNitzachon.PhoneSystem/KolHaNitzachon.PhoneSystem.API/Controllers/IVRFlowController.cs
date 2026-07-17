using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Repositories;
using KolHaNitzachon.PhoneSystem.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KolHaNitzachon.PhoneSystem.API.Controllers
{
    [ApiController]
    [Route("api/ivr")]
    public class IVRFlowController : ControllerBase
    {
        private readonly IMenuRenderer _menuRenderer;
        private readonly IRecipientRepository _recipientRepository;
        private readonly ILogger<IVRFlowController> _logger;

        public IVRFlowController(
            IMenuRenderer menuRenderer,
            IRecipientRepository recipientRepository,
            ILogger<IVRFlowController> logger)
        {
            _menuRenderer = menuRenderer;
            _recipientRepository = recipientRepository;
            _logger = logger;
        }

        [HttpPost("handle-call")]
        public async Task<IActionResult> HandleCall(
            [FromQuery] string? step,
            [FromQuery] Guid? recipientId,
            [FromForm(Name = "CallSid")] string? callSid,
            [FromForm(Name = "From")] string? from,
            [FromForm(Name = "Digits")] string? digits)
        {
            try
            {
                step = string.IsNullOrWhiteSpace(step)
                    ? "main"
                    : step.Trim().ToLowerInvariant();

                digits = NormalizeDigits(digits);

                var baseUrl =
                    $"{Request.Scheme}://{Request.Host}";

                /*
                 * TEMPORARY LOCAL RECORDING LOCATION
                 *
                 * Files are currently served from:
                 * API/wwwroot/recordings/
                 *
                 * After Azure is configured, MenuRenderer can use
                 * BlobStorageService.GenerateSasUrl instead.
                 */
                var recordingBaseUrl =
                    $"{baseUrl}/recordings";

                _logger.LogInformation(
                    "IVR webhook. Step={Step}, CallSid={CallSid}, " +
                    "From={From}, Digits={Digits}, RecipientId={RecipientId}",
                    step,
                    callSid ?? "none",
                    from ?? "none",
                    digits ?? "none",
                    recipientId);

                switch (step)
                {
                    case "main":
                        return await HandleMainMenuAsync(
                            digits,
                            baseUrl,
                            recordingBaseUrl);

                    case "sponsor-all":
                        return HandleSponsorAllMenu(
                            digits,
                            baseUrl,
                            recordingBaseUrl);

                    case "sponsor-specific":
                        return HandleSponsorSpecificMenu(
                            digits,
                            baseUrl,
                            recordingBaseUrl);

                    case "contestant-code":
                        return await HandleContestantCodeAsync(
                            digits,
                            baseUrl,
                            recordingBaseUrl);

                    case "contestant-list":
                        return await HandleContestantListAsync(
                            digits,
                            baseUrl,
                            recordingBaseUrl);

                    case "pledge-amount":
                        return await HandlePledgeAmountAsync(
                            digits,
                            recipientId,
                            baseUrl,
                            recordingBaseUrl);

                    default:
                        return Xml(
                            _menuRenderer.RenderInvalidOption(
                                BuildActionUrl(
                                    baseUrl,
                                    "main"),
                                recordingBaseUrl));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled error while processing IVR webhook.");

                var errorResponse =
                    new Twilio.TwiML.VoiceResponse();

                errorResponse.Say(
                    "We are currently experiencing a technical issue. " +
                    "Please try again later.");

                errorResponse.Hangup();

                return Xml(errorResponse);
            }
        }

        private async Task<IActionResult> HandleMainMenuAsync(
            string? digits,
            string baseUrl,
            string recordingBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(digits))
            {
                return Xml(
                    _menuRenderer.RenderMainMenu(
                        BuildActionUrl(
                            baseUrl,
                            "main"),
                        recordingBaseUrl));
            }

            return digits switch
            {
                "1" => Xml(
                    _menuRenderer.RenderSponsorAllMenu(
                        BuildActionUrl(
                            baseUrl,
                            "sponsor-all"),
                        recordingBaseUrl)),

                "2" => Xml(
                    _menuRenderer.RenderSponsorSpecificMenu(
                        BuildActionUrl(
                            baseUrl,
                            "sponsor-specific"),
                        recordingBaseUrl)),

                "3" => await HandleContestantListAsync(
                    null,
                    baseUrl,
                    recordingBaseUrl),

                _ => Xml(
                    _menuRenderer.RenderInvalidOption(
                        BuildActionUrl(
                            baseUrl,
                            "main"),
                        recordingBaseUrl))
            };
        }

        private IActionResult HandleSponsorAllMenu(
            string? digits,
            string baseUrl,
            string recordingBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(digits))
            {
                return Xml(
                    _menuRenderer.RenderSponsorAllMenu(
                        BuildActionUrl(
                            baseUrl,
                            "sponsor-all"),
                        recordingBaseUrl));
            }

            /*
             * Sponsor-all calculation will be added after
             * the contestant-specific flow is stable.
             */
            return Xml(
                _menuRenderer.RenderInvalidOption(
                    BuildActionUrl(
                        baseUrl,
                        "sponsor-all"),
                    recordingBaseUrl));
        }

        private IActionResult HandleSponsorSpecificMenu(
            string? digits,
            string baseUrl,
            string recordingBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(digits))
            {
                return Xml(
                    _menuRenderer.RenderSponsorSpecificMenu(
                        BuildActionUrl(
                            baseUrl,
                            "sponsor-specific"),
                        recordingBaseUrl));
            }

            return digits switch
            {
                // Enter contestant code.
                "1" => Xml(
                    _menuRenderer.RenderEnterContestantCode(
                        BuildActionUrl(
                            baseUrl,
                            "contestant-code"),
                        recordingBaseUrl)),

                // Hear the active contestant list.
                "2" => RedirectToStep(
                    baseUrl,
                    "contestant-list"),

                _ => Xml(
                    _menuRenderer.RenderInvalidOption(
                        BuildActionUrl(
                            baseUrl,
                            "sponsor-specific"),
                        recordingBaseUrl))
            };
        }

        private async Task<IActionResult> HandleContestantCodeAsync(
            string? digits,
            string baseUrl,
            string recordingBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(digits))
            {
                return Xml(
                    _menuRenderer.RenderEnterContestantCode(
                        BuildActionUrl(
                            baseUrl,
                            "contestant-code"),
                        recordingBaseUrl));
            }

            if (!int.TryParse(
                    digits,
                    out var contestantCode))
            {
                return Xml(
                    _menuRenderer.RenderContestantNotFound(
                        BuildActionUrl(
                            baseUrl,
                            "contestant-code"),
                        recordingBaseUrl));
            }

            var recipients =
                await _recipientRepository.GetAllAsync();

            var recipient = recipients.FirstOrDefault(
                x => x.Code == contestantCode &&
                     IsRecipientActive(x));

            if (recipient == null)
            {
                return Xml(
                    _menuRenderer.RenderContestantNotFound(
                        BuildActionUrl(
                            baseUrl,
                            "contestant-code"),
                        recordingBaseUrl));
            }

            var amountActionUrl =
                BuildActionUrl(
                    baseUrl,
                    "pledge-amount",
                    recipient.Id);

            return Xml(
                _menuRenderer.RenderContestantDonation(
                    recipient,
                    amountActionUrl,
                    recordingBaseUrl));
        }

        private async Task<IActionResult> HandleContestantListAsync(
            string? digits,
            string baseUrl,
            string recordingBaseUrl)
        {
            /*
             * When the list finishes, the caller may immediately type
             * a contestant code followed by #.
             */
            if (!string.IsNullOrWhiteSpace(digits))
            {
                return await HandleContestantCodeAsync(
                    digits,
                    baseUrl,
                    recordingBaseUrl);
            }

            var recipients =
                await _recipientRepository.GetAllAsync();

            return Xml(
                _menuRenderer.RenderContestantList(
                    recipients,
                    BuildActionUrl(
                        baseUrl,
                        "contestant-list"),
                    recordingBaseUrl));
        }

        private async Task<IActionResult> HandlePledgeAmountAsync(
            string? digits,
            Guid? recipientId,
            string baseUrl,
            string recordingBaseUrl)
        {
            if (!recipientId.HasValue)
            {
                return Xml(
                    _menuRenderer.RenderInvalidOption(
                        BuildActionUrl(
                            baseUrl,
                            "main"),
                        recordingBaseUrl));
            }

            var recipient =
                await _recipientRepository.GetByIdAsync(
                    recipientId.Value);

            if (recipient == null)
            {
                return Xml(
                    _menuRenderer.RenderContestantNotFound(
                        BuildActionUrl(
                            baseUrl,
                            "contestant-code"),
                        recordingBaseUrl));
            }

            if (string.IsNullOrWhiteSpace(digits))
            {
                return Xml(
                    _menuRenderer.RenderContestantDonation(
                        recipient,
                        BuildActionUrl(
                            baseUrl,
                            "pledge-amount",
                            recipient.Id),
                        recordingBaseUrl));
            }

            if (!decimal.TryParse(
                    digits,
                    out var pledgeAmount) ||
                pledgeAmount <= 0)
            {
                return Xml(
                    _menuRenderer.RenderInvalidOption(
                        BuildActionUrl(
                            baseUrl,
                            "pledge-amount",
                            recipient.Id),
                        recordingBaseUrl));
            }

            /*
             * This is where the Cardknox/Sola payment flow will begin.
             * For now, we confirm the dynamically entered amount.
             */
            return Xml(
                _menuRenderer.RenderPledgeConfirmation(
                    recipient,
                    pledgeAmount,
                    BuildActionUrl(
                        baseUrl,
                        "main"),
                    recordingBaseUrl));
        }

        private ContentResult Xml(
            Twilio.TwiML.VoiceResponse response)
        {
            return Content(
                response.ToString(),
                "application/xml");
        }

        private IActionResult RedirectToStep(
            string baseUrl,
            string step)
        {
            var response =
                new Twilio.TwiML.VoiceResponse();

            response.Redirect(
                new Uri(
                    BuildActionUrl(baseUrl, step),
                    UriKind.Absolute),
                method: "POST");

            return Xml(response);
        }

        private static string BuildActionUrl(
            string baseUrl,
            string step,
            Guid? recipientId = null)
        {
            var url =
                $"{baseUrl.TrimEnd('/')}" +
                $"/api/ivr/handle-call" +
                $"?step={Uri.EscapeDataString(step)}";

            if (recipientId.HasValue)
            {
                url +=
                    $"&recipientId={recipientId.Value}";
            }

            return url;
        }

        private static string? NormalizeDigits(
            string? digits)
        {
            if (string.IsNullOrWhiteSpace(digits) ||
                digits.Equals(
                    "string",
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new string(
                digits
                    .Where(char.IsDigit)
                    .ToArray());
        }

        private static bool IsRecipientActive(
            Recipient recipient)
        {
            var today = DateTime.UtcNow.Date;

            return recipient.StartDate.Date <= today &&
                   (!recipient.EndDate.HasValue ||
                    recipient.EndDate.Value.Date >= today);
        }
    }
}