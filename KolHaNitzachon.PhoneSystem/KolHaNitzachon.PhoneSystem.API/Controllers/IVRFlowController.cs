using Azure.Storage.Blobs;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Repositories;
using KolHaNitzachon.PhoneSystem.Application.Models;
using KolHaNitzachon.PhoneSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Twilio.TwiML;
using KolHaNitzachon.PhoneSystem.Shared.Constants;

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

        private readonly IConfiguration _configuration;

        public IVRFlowController(
            IMenuRenderer menuRenderer,
            IIvrCallSessionStore sessionStore,
            ILogger<IVRFlowController> logger,
            IConfiguration configuration)
        {
            _menuRenderer = menuRenderer;
            _sessionStore = sessionStore;
            _logger = logger;
            _configuration = configuration; 
        }

        [HttpPost("handle-call")]
        [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
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

                var safeDigitsForLog = IsPaymentStep(step)
                    ? string.IsNullOrWhiteSpace(digits)
                        ? "none"
                        : "[REDACTED]"
                    : digits ?? "none";

                _logger.LogInformation(
                    "IVR request. Step={Step}, CallSid={CallSid}, " +
                    "From={From}, Digits={Digits}, " +
                    "DonationType={DonationType}, " +
                    "DonationAmount={DonationAmount}",
                    step,
                    effectiveCallSid,
                    from ?? "none",
                    safeDigitsForLog,
                    session.DonationType,
                    session.DonationAmount);

                return step switch
                {
                    IvrSteps.Main => HandleMainMenu(
                        session,
                        digits,
                        applicationBaseUrl,
                        recordingBaseUrl),

                    IvrSteps.SponsorAll => HandleSponsorAll(
                        session,
                        digits,
                        applicationBaseUrl,
                        recordingBaseUrl),

                    IvrSteps.DonationAmount => HandleDonationAmount(
                        session,
                        digits,
                        applicationBaseUrl,
                        recordingBaseUrl),

                    IvrSteps.ConfirmDonation => HandleDonationConfirmation(
                        session,
                        digits,
                        applicationBaseUrl,
                        recordingBaseUrl),

                    IvrSteps.PaymentCardNumber => HandlePaymentCardNumber(
                        session,
                        digits,
                        applicationBaseUrl),

                    IvrSteps.PaymentExpiry => HandlePaymentExpiry(
                        session,
                        digits,
                        applicationBaseUrl),

                    IvrSteps.PaymentCvv => HandlePaymentCvv(
                        session,
                        digits,
                        applicationBaseUrl),

                    IvrSteps.EndCall => EndCall(
                        session,
                        recordingBaseUrl),

                    _ => Xml(
                        _menuRenderer.RenderInvalidOption(
                            BuildActionUrl(
                                applicationBaseUrl,
                                IvrSteps.Main)))
                };

                //return step switch
                //{
                //    "main" => HandleMainMenu(
                //        session,
                //        digits,
                //        applicationBaseUrl,
                //        recordingBaseUrl),

                //    "sponsor-all" => HandleSponsorAll(
                //        session,
                //        digits,
                //        applicationBaseUrl,
                //        recordingBaseUrl),

                //    "donation-amount" => HandleDonationAmount(
                //        session,
                //        digits,
                //        applicationBaseUrl,
                //        recordingBaseUrl),

                //    "confirm-donation" => HandleDonationConfirmation(
                //        session,
                //        digits,
                //        applicationBaseUrl,
                //        recordingBaseUrl),

                //    "payment-card-number" => HandlePaymentCardNumber(
                //        session,
                //        digits,
                //        applicationBaseUrl),

                //    "payment-expiry" => HandlePaymentExpiry(
                //        session,
                //        digits,
                //        applicationBaseUrl),

                //    "payment-cvv" => HandlePaymentCvv(
                //        session,
                //        digits,
                //        applicationBaseUrl),

                //    "end-call" => EndCall(
                //        session,
                //        recordingBaseUrl),

                //    _ => Xml(
                //        _menuRenderer.RenderInvalidOption(
                //            BuildActionUrl(
                //                applicationBaseUrl,
                //                "main")))
                //};
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
                    MaskedCardNumber = MaskCardNumber(session.CardNumber),
                    HasExpiryDate = !string.IsNullOrWhiteSpace(session.ExpiryMMYY),
                    HasCvv = !string.IsNullOrWhiteSpace(session.Cvv),
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

        private static bool IsPaymentStep(string step)
        {
            return step.StartsWith(
                "payment-",
                StringComparison.OrdinalIgnoreCase);
        }

        private static string? MaskCardNumber(string? cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                return null;
            }

            var lastFour = cardNumber.Length <= 4
                ? cardNumber
                : cardNumber[^4..];

            return $"**** **** **** {lastFour}";
        }

        private static bool IsValidCardNumberLength(string cardNumber)
        {
            return cardNumber.Length is >= 13 and <= 19;
        }

        //private static bool IsPaymentStep(string step)
        //{
        //    return step.StartsWith(
        //        "payment-",
        //        StringComparison.OrdinalIgnoreCase);
        //}

        private static bool IsValidExpiryDate(string expiryMMYY)
        {
            if (expiryMMYY.Length != 4)
            {
                return false;
            }

            if (!int.TryParse(expiryMMYY[..2], out var month))
            {
                return false;
            }

            if (!int.TryParse(expiryMMYY[2..], out var twoDigitYear))
            {
                return false;
            }

            if (month is < 1 or > 12)
            {
                return false;
            }

            var currentDate = DateTime.UtcNow;
            var currentTwoDigitYear = currentDate.Year % 100;

            if (twoDigitYear < currentTwoDigitYear)
            {
                return false;
            }

            if (twoDigitYear == currentTwoDigitYear &&
                month < currentDate.Month)
            {
                return false;
            }

            if (twoDigitYear > currentTwoDigitYear + 20)
            {
                return false;
            }

            return true;
        }

        private static bool IsValidCvv(string cvv)
        {
            return cvv.Length is 3 or 4 &&
                   cvv.All(char.IsDigit);
        }

        private IActionResult HandlePaymentCardNumber(IvrCallSession session, string? digits, string applicationBaseUrl)
        {
            var cardNumberActionUrl = BuildActionUrl(
                applicationBaseUrl,
                IvrSteps.PaymentCardNumber);

            if (string.IsNullOrWhiteSpace(digits))
            {
                session.CurrentStep = IvrSteps.PaymentCardNumber;

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderEnterCardNumber(
                        cardNumberActionUrl));
            }

            var cleanedCardNumber = new string(
                digits
                    .Where(char.IsDigit)
                    .ToArray());

            if (!IsValidCardNumberLength(cleanedCardNumber))
            {
                session.CardNumber = null;
                session.CurrentStep = IvrSteps.PaymentCardNumber;

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderInvalidCardNumber(
                        cardNumberActionUrl));
            }

            session.CardNumber = cleanedCardNumber;
            session.ExpiryMMYY = null;
            session.CurrentStep = IvrSteps.PaymentExpiry;

            _sessionStore.Update(session);

            return Xml(
                _menuRenderer.RenderEnterExpiryDate(
                    BuildActionUrl(
                        applicationBaseUrl,
                        IvrSteps.PaymentExpiry)));
        }

        private IActionResult HandlePaymentExpiry(IvrCallSession session, string? digits, string applicationBaseUrl)
        {
            var expiryActionUrl = BuildActionUrl(
                applicationBaseUrl,
                IvrSteps.PaymentExpiry);

            // The caller must enter a card number before entering expiry.
            if (string.IsNullOrWhiteSpace(session.CardNumber))
            {
                session.ExpiryMMYY = null;
                session.CurrentStep = IvrSteps.PaymentCardNumber;

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderEnterCardNumber(
                        BuildActionUrl(
                            applicationBaseUrl,
                            IvrSteps.PaymentCardNumber)));
            }

            // No digits means the prompt was opened directly
            // or the caller did not enter anything.
            if (string.IsNullOrWhiteSpace(digits))
            {
                session.CurrentStep = IvrSteps.PaymentExpiry;

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderEnterExpiryDate(
                        expiryActionUrl));
            }

            var cleanedExpiry = new string(
                digits
                    .Where(char.IsDigit)
                    .ToArray());

            if (!IsValidExpiryDate(cleanedExpiry))
            {
                session.ExpiryMMYY = null;
                session.CurrentStep = IvrSteps.PaymentExpiry;

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderInvalidExpiryDate(
                        expiryActionUrl));
            }

            session.ExpiryMMYY = cleanedExpiry;
            session.Cvv = null;
            session.CurrentStep = IvrSteps.PaymentCvv;

            _sessionStore.Update(session);

            return Xml(
                _menuRenderer.RenderEnterCvv(
                    BuildActionUrl(
                        applicationBaseUrl,
                        IvrSteps.PaymentCvv)));
        }

        private IActionResult HandlePaymentCvv(IvrCallSession session, string? digits, string applicationBaseUrl)
        {
            var cvvActionUrl = BuildActionUrl(
                applicationBaseUrl,
                IvrSteps.PaymentCvv);

            /*
             * The caller must complete the card-number
             * and expiry stages before entering CVV.
             */
            if (string.IsNullOrWhiteSpace(session.CardNumber))
            {
                session.Cvv = null;
                session.CurrentStep = IvrSteps.PaymentCardNumber;

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderEnterCardNumber(
                        BuildActionUrl(
                            applicationBaseUrl,
                            IvrSteps.PaymentCardNumber)));
            }

            if (string.IsNullOrWhiteSpace(session.ExpiryMMYY))
            {
                session.Cvv = null;
                session.CurrentStep = IvrSteps.PaymentExpiry;

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderEnterExpiryDate(
                        BuildActionUrl(
                            applicationBaseUrl,
                            IvrSteps.PaymentExpiry)));
            }

            if (string.IsNullOrWhiteSpace(digits))
            {
                session.CurrentStep = IvrSteps.PaymentCvv;

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderEnterCvv(
                        cvvActionUrl));
            }

            var cleanedCvv = new string(
                digits
                    .Where(char.IsDigit)
                    .ToArray());

            if (!IsValidCvv(cleanedCvv))
            {
                session.Cvv = null;
                session.CurrentStep = IvrSteps.PaymentCvv;

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderInvalidCvv(
                        cvvActionUrl));
            }

            session.Cvv = cleanedCvv;
            session.CurrentStep = IvrSteps.PaymentZip;

            _sessionStore.Update(session);

            /*
             * Temporary stopping point until billing ZIP
             * collection is implemented.
             */
            var response = new VoiceResponse();

            response.Say(
                "Your card security code was received. " +
                "The billing postal code step will be added next.");

            response.Hangup();

            return Xml(response);
        }

        private IActionResult HandleDonationConfirmation(IvrCallSession session, string? digits, string applicationBaseUrl, string recordingBaseUrl)
        {
            if (session.DonationAmount is null)
            {
                _logger.LogWarning(
                    "Donation confirmation requested without " +
                    "an amount. CallSid={CallSid}",
                    session.CallSid);

                session.CurrentStep = IvrSteps.DonationAmount;

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderEnterDonationAmount(
                        BuildActionUrl(
                            applicationBaseUrl,
                            IvrSteps.DonationAmount),
                        recordingBaseUrl));
            }

            var confirmationActionUrl = BuildActionUrl(
                applicationBaseUrl,
                IvrSteps.ConfirmDonation);

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
                    session.CurrentStep = IvrSteps.PaymentCardNumber;
                    _sessionStore.Update(session);
                    return Xml(
                        _menuRenderer.RenderEnterCardNumber(
                            BuildActionUrl(
                                applicationBaseUrl,
                                IvrSteps.PaymentCardNumber)));

                case "2":
                    session.DonationAmount = null;
                    session.CurrentStep = IvrSteps.DonationAmount;
                    _sessionStore.Update(session);
                    return Xml(
                        _menuRenderer.RenderEnterDonationAmount(
                            BuildActionUrl(
                                applicationBaseUrl,
                                IvrSteps.DonationAmount),
                            recordingBaseUrl));

                case "9":
                    ResetSessionForMainMenu(session);
                    return Xml(
                        _menuRenderer.RenderMainMenu(
                            BuildActionUrl(
                                applicationBaseUrl,
                                IvrSteps.Main),
                            recordingBaseUrl));

                default:
                    return Xml(
                        _menuRenderer.RenderInvalidOption(
                            confirmationActionUrl));
            }
        }

        private void ResetSessionForMainMenu(IvrCallSession session)
        {
            session.CurrentStep = IvrSteps.Main;
            session.DonationType = null;
            session.DonationAmount = null;
            session.RecipientId = null;
            session.RecipientCode = null;

            session.CardNumber = null;
            session.ExpiryMMYY = null;
            session.Cvv = null;
            session.BillingZip = null;

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
                        IvrSteps.Main),
                    recordingBaseUrl));
        }

        [HttpGet("debug/blob-storage")]
        public async Task<IActionResult> TestBlobStorage()
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(
                    _configuration.GetConnectionString("AzureBlobStorage"));

                var containerClient =
                    blobServiceClient.GetBlobContainerClient("ivrrecordings");

                var exists =
                    await containerClient.ExistsAsync();

                return Ok(new
                {
                    Container = "ivrrecordings",
                    Exists = exists.Value
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Error = ex.Message
                });
            }
        }

        private IActionResult HandleMainMenu(IvrCallSession session, string? digits, string applicationBaseUrl, string recordingBaseUrl)
        {
            var actionUrl = BuildActionUrl(
                applicationBaseUrl,
                IvrSteps.Main);

            if (string.IsNullOrWhiteSpace(digits))
            {
                session.CurrentStep = IvrSteps.Main;

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
            session.CurrentStep = IvrSteps.SponsorAll;

            // Clear values left over if the caller restarted the menu.
            session.DonationType = null;
            session.DonationAmount = null;
            session.RecipientId = null;
            session.RecipientCode = null;

            session.CardNumber = null;
            session.ExpiryMMYY = null;
            session.Cvv = null;
            session.BillingZip = null;

            _sessionStore.Update(session);

            return Xml(
                _menuRenderer.RenderSponsorAllMenu(
                    BuildActionUrl(
                        applicationBaseUrl,
                        IvrSteps.SponsorAll),
                    recordingBaseUrl));
        }

        private IActionResult HandleSponsorAll(IvrCallSession session, string? digits, string applicationBaseUrl, string recordingBaseUrl)
        {
            var currentActionUrl = BuildActionUrl(
                applicationBaseUrl,
                IvrSteps.SponsorAll);

            if (string.IsNullOrWhiteSpace(digits))
            {
                session.CurrentStep = IvrSteps.SponsorAll;

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

            session.CurrentStep = IvrSteps.DonationAmount;
            session.DonationAmount = null;

            _sessionStore.Update(session);

            return Xml(
                _menuRenderer.RenderEnterDonationAmount(
                    BuildActionUrl(
                        applicationBaseUrl,
                        IvrSteps.DonationAmount),
                    recordingBaseUrl));
        }

        private IActionResult HandleDonationAmount(IvrCallSession session, string? digits, string applicationBaseUrl, string recordingBaseUrl)
        {
            var donationActionUrl = BuildActionUrl(
                applicationBaseUrl,
                IvrSteps.DonationAmount);

            if (session.DonationType is null or
                DonationType.Unknown)
            {
                _logger.LogWarning(
                    "Donation amount requested without a donation type. " +
                    "CallSid={CallSid}",
                    session.CallSid);

                session.CurrentStep = IvrSteps.SponsorAll;

                _sessionStore.Update(session);

                return Xml(
                    _menuRenderer.RenderSponsorAllMenu(
                        BuildActionUrl(
                            applicationBaseUrl,
                            IvrSteps.SponsorAll),
                        recordingBaseUrl));
            }

            if (string.IsNullOrWhiteSpace(digits))
            {
                session.CurrentStep = IvrSteps.DonationAmount;

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
            session.CurrentStep = IvrSteps.ConfirmDonation;

            _sessionStore.Update(session);

            return Xml(
                _menuRenderer.RenderDonationConfirmation(
                    donationAmount,
                    BuildActionUrl(
                        applicationBaseUrl,
                        IvrSteps.ConfirmDonation),
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
                ? IvrSteps.Main
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