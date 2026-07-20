using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Repositories;
using KolHaNitzachon.PhoneSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Twilio.TwiML;

namespace KolHaNitzachon.PhoneSystem.API.Controllers
{
    [ApiController]
    [Route("api/ivr")]
    [Produces("application/xml")]
    public class IVRFlowController : ControllerBase
    {
        private readonly IMenuRenderer _menuRenderer;
        private readonly ILogger<IVRFlowController> _logger;

        /*
         * TODO (PRODUCTION):
         * Re-enable this dependency when the IVR is switched from temporary
         * in-memory test data to the SQL/EF Core RecipientRepository.
         */
        // private readonly IRecipientRepository _recipientRepository;

        /*
         * ================================================================
         * TEMPORARY IVR TEST DATA
         * ================================================================
         *
         * Purpose:
         * Allows the complete IVR menu flow to be tested before the live
         * repository is enabled.
         *
         * Production replacement:
         *
         * 1. Re-enable IRecipientRepository above and in the constructor.
         * 2. Replace TestRecipients lookups with:
         *
         *    await _recipientRepository.GetAllAsync(...)
         *    await _recipientRepository.GetByIdAsync(...)
         *    await _recipientRepository.GetByCodeAsync(...)
         *
         * 3. In Program.cs, replace InMemoryRecipientRepository registration
         *    with RecipientRepository.
         *
         * ================================================================
         */
        private static readonly IReadOnlyCollection<Recipient> TestRecipients =
            new List<Recipient>
            {
                new()
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Code = 203,
                    Name = "John Smith",
                    StartDate = DateTime.UtcNow.Date.AddDays(-16),
                    EndDate = DateTime.UtcNow.Date.AddMonths(1),
                    NameRecordingUrl = "JohnSmith.mp3"
                },
                new()
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Code = 301,
                    Name = "David Cohen",
                    StartDate = DateTime.UtcNow.Date.AddDays(-8),
                    EndDate = DateTime.UtcNow.Date.AddMonths(1),
                    NameRecordingUrl = "DavidCohen.mp3"
                }
            };

        public IVRFlowController(
            IMenuRenderer menuRenderer,
            ILogger<IVRFlowController> logger
            /*
             * TODO (PRODUCTION):
             * Add this parameter back:
             *
             * , IRecipientRepository recipientRepository
             */
        )
        {
            _menuRenderer = menuRenderer;
            _logger = logger;

            /*
             * TODO (PRODUCTION):
             * Restore this assignment:
             *
             * _recipientRepository = recipientRepository;
             */
        }

        [HttpPost("handle-call")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> HandleCall(
            [FromQuery] string? step,
            [FromQuery] Guid? recipientId,
            [FromForm(Name = "CallSid")] string? callSid,
            [FromForm(Name = "From")] string? from,
            [FromForm(Name = "Digits")] string? digits,
            CancellationToken cancellationToken)
        {
            try
            {
                step = NormalizeStep(step);
                digits = NormalizeDigits(digits);

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var recordingBaseUrl = $"{baseUrl}/recordings";

                _logger.LogInformation(
                    "IVR request received. Step={Step}, CallSid={CallSid}, " +
                    "From={From}, Digits={Digits}, RecipientId={RecipientId}",
                    step,
                    callSid ?? "none",
                    from ?? "none",
                    digits ?? "none",
                    recipientId);

                return step switch
                {
                    "main" => await HandleMainMenuAsync(
                        digits,
                        baseUrl,
                        recordingBaseUrl,
                        cancellationToken),

                    "sponsor-all" => HandleSponsorAllMenu(
                        digits,
                        baseUrl,
                        recordingBaseUrl),

                    "sponsor-specific" => HandleSponsorSpecificMenu(
                        digits,
                        baseUrl,
                        recordingBaseUrl),

                    "contestant-code" => await HandleContestantCodeAsync(
                        digits,
                        baseUrl,
                        recordingBaseUrl,
                        cancellationToken),

                    "contestant-list" => await HandleContestantListAsync(
                        digits,
                        baseUrl,
                        recordingBaseUrl,
                        cancellationToken),

                    "pledge-amount" => await HandlePledgeAmountAsync(
                        digits,
                        recipientId,
                        baseUrl,
                        recordingBaseUrl,
                        cancellationToken),

                    _ => Xml(
                        _menuRenderer.RenderInvalidOption(
                            BuildActionUrl(baseUrl, "main"),
                            recordingBaseUrl))
                };
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled error while processing the IVR request.");

                var errorResponse = new VoiceResponse();
                errorResponse.Say(
                    "We are currently experiencing a technical issue. " +
                    "Please try again later.");
                errorResponse.Hangup();

                return Xml(errorResponse);
            }
        }

        private Task<IActionResult> HandleMainMenuAsync(
            string? digits,
            string baseUrl,
            string recordingBaseUrl,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IActionResult result;

            if (string.IsNullOrWhiteSpace(digits))
            {
                result = Xml(
                    _menuRenderer.RenderMainMenu(
                        BuildActionUrl(baseUrl, "main"),
                        recordingBaseUrl));

                return Task.FromResult(result);
            }

            result = digits switch
            {
                "1" => Xml(
                    _menuRenderer.RenderSponsorAllMenu(
                        BuildActionUrl(baseUrl, "sponsor-all"),
                        recordingBaseUrl)),

                "2" => Xml(
                    _menuRenderer.RenderSponsorSpecificMenu(
                        BuildActionUrl(baseUrl, "sponsor-specific"),
                        recordingBaseUrl)),

                "3" => RedirectToStep(baseUrl, "contestant-list"),

                _ => Xml(
                    _menuRenderer.RenderInvalidOption(
                        BuildActionUrl(baseUrl, "main"),
                        recordingBaseUrl))
            };

            return Task.FromResult(result);
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
                        BuildActionUrl(baseUrl, "sponsor-all"),
                        recordingBaseUrl));
            }

            // TODO: Implement sponsor-all calculations and amount collection.
            return Xml(
                _menuRenderer.RenderInvalidOption(
                    BuildActionUrl(baseUrl, "sponsor-all"),
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
                        BuildActionUrl(baseUrl, "sponsor-specific"),
                        recordingBaseUrl));
            }

            return digits switch
            {
                "1" => Xml(
                    _menuRenderer.RenderEnterContestantCode(
                        BuildActionUrl(baseUrl, "contestant-code"),
                        recordingBaseUrl)),

                "2" => RedirectToStep(baseUrl, "contestant-list"),

                _ => Xml(
                    _menuRenderer.RenderInvalidOption(
                        BuildActionUrl(baseUrl, "sponsor-specific"),
                        recordingBaseUrl))
            };
        }

        private Task<IActionResult> HandleContestantCodeAsync(
            string? digits,
            string baseUrl,
            string recordingBaseUrl,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(digits))
            {
                return Task.FromResult<IActionResult>(
                    Xml(
                        _menuRenderer.RenderEnterContestantCode(
                            BuildActionUrl(baseUrl, "contestant-code"),
                            recordingBaseUrl)));
            }

            if (!int.TryParse(digits, out var contestantCode))
            {
                return Task.FromResult<IActionResult>(
                    Xml(
                        _menuRenderer.RenderContestantNotFound(
                            BuildActionUrl(baseUrl, "contestant-code"),
                            recordingBaseUrl)));
            }

            /*
             * TEMPORARY TEST LOOKUP
             *
             * TODO (PRODUCTION): Replace with:
             *
             * var recipient =
             *     await _recipientRepository.GetByCodeAsync(
             *         contestantCode,
             *         cancellationToken);
             */
            var recipient = TestRecipients.FirstOrDefault(
                x => x.Code == contestantCode && IsRecipientActive(x));

            if (recipient is null)
            {
                return Task.FromResult<IActionResult>(
                    Xml(
                        _menuRenderer.RenderContestantNotFound(
                            BuildActionUrl(baseUrl, "contestant-code"),
                            recordingBaseUrl)));
            }

            var pledgeAmountActionUrl =
                BuildActionUrl(
                    baseUrl,
                    "pledge-amount",
                    recipient.Id);

            return Task.FromResult<IActionResult>(
                Xml(
                    _menuRenderer.RenderContestantDonation(
                        recipient,
                        pledgeAmountActionUrl,
                        recordingBaseUrl)));
        }

        private async Task<IActionResult> HandleContestantListAsync(
            string? digits,
            string baseUrl,
            string recordingBaseUrl,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(digits))
            {
                return await HandleContestantCodeAsync(
                    digits,
                    baseUrl,
                    recordingBaseUrl,
                    cancellationToken);
            }

            /*
             * TEMPORARY TEST LOOKUP
             *
             * TODO (PRODUCTION): Replace with:
             *
             * var recipients =
             *     await _recipientRepository.GetAllAsync(cancellationToken);
             */
            var activeRecipients = TestRecipients
                .Where(IsRecipientActive)
                .OrderBy(x => x.Name)
                .ToList();

            return Xml(
                _menuRenderer.RenderContestantList(
                    activeRecipients,
                    BuildActionUrl(baseUrl, "contestant-list"),
                    recordingBaseUrl));
        }

        private Task<IActionResult> HandlePledgeAmountAsync(
            string? digits,
            Guid? recipientId,
            string baseUrl,
            string recordingBaseUrl,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!recipientId.HasValue)
            {
                return Task.FromResult<IActionResult>(
                    Xml(
                        _menuRenderer.RenderInvalidOption(
                            BuildActionUrl(baseUrl, "main"),
                            recordingBaseUrl)));
            }

            /*
             * TEMPORARY TEST LOOKUP
             *
             * TODO (PRODUCTION): Replace with:
             *
             * var recipient =
             *     await _recipientRepository.GetByIdAsync(
             *         recipientId.Value,
             *         cancellationToken);
             */
            var recipient = TestRecipients.FirstOrDefault(
                x => x.Id == recipientId.Value && IsRecipientActive(x));

            if (recipient is null)
            {
                return Task.FromResult<IActionResult>(
                    Xml(
                        _menuRenderer.RenderContestantNotFound(
                            BuildActionUrl(baseUrl, "contestant-code"),
                            recordingBaseUrl)));
            }

            if (string.IsNullOrWhiteSpace(digits))
            {
                return Task.FromResult<IActionResult>(
                    Xml(
                        _menuRenderer.RenderContestantDonation(
                            recipient,
                            BuildActionUrl(
                                baseUrl,
                                "pledge-amount",
                                recipient.Id),
                            recordingBaseUrl)));
            }

            if (!decimal.TryParse(digits, out var pledgeAmount) ||
                pledgeAmount <= 0)
            {
                return Task.FromResult<IActionResult>(
                    Xml(
                        _menuRenderer.RenderInvalidOption(
                            BuildActionUrl(
                                baseUrl,
                                "pledge-amount",
                                recipient.Id),
                            recordingBaseUrl)));
            }

            /*
             * Current testing endpoint:
             * confirms the entered pledge amount using TTS.
             *
             * TODO (PRODUCTION):
             * Continue to the Cardknox/Sola payment collection step.
             */
            return Task.FromResult<IActionResult>(
                Xml(
                    _menuRenderer.RenderPledgeConfirmation(
                        recipient,
                        pledgeAmount,
                        BuildActionUrl(baseUrl, "main"),
                        recordingBaseUrl)));
        }

        private ContentResult Xml(VoiceResponse response)
        {
            return Content(
                response.ToString(),
                "application/xml");
        }

        private IActionResult RedirectToStep(
            string baseUrl,
            string step)
        {
            var response = new VoiceResponse();

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
                "/api/ivr/handle-call" +
                $"?step={Uri.EscapeDataString(step)}";

            if (recipientId.HasValue)
            {
                url += $"&recipientId={recipientId.Value}";
            }

            return url;
        }

        private static string NormalizeStep(string? step)
        {
            if (string.IsNullOrWhiteSpace(step) ||
                step.Equals(
                    "string",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "main";
            }

            return step.Trim().ToLowerInvariant();
        }

        private static string? NormalizeDigits(string? digits)
        {
            if (string.IsNullOrWhiteSpace(digits) ||
                digits.Equals(
                    "string",
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var numericDigits = new string(
                digits.Where(char.IsDigit).ToArray());

            return string.IsNullOrWhiteSpace(numericDigits)
                ? null
                : numericDigits;
        }

        private static bool IsRecipientActive(Recipient recipient)
        {
            var today = DateTime.UtcNow.Date;

            return recipient.StartDate.Date <= today &&
                   (!recipient.EndDate.HasValue ||
                    recipient.EndDate.Value.Date >= today);
        }
    }
}
