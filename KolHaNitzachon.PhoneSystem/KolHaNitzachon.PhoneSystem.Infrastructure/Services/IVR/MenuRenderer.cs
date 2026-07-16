using KolHaNitzachon.PhoneSystem.Application.Interfaces.External;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Services.IVR
{
    public class MenuRenderer : IMenuRenderer
    {
        //private readonly IBlobStorageService _blobStorageService;

        public MenuRenderer()//(IBlobStorageService blobStorageService)
        {
            //_blobStorageService = blobStorageService;
        }

        public VoiceResponse RenderMainMenu(string? digits)
        {
            var response = new VoiceResponse();

            // -------------------------------------------------------
            // TEMP URL
            // Replace with production URL after deployment.
            // -------------------------------------------------------
            //var actionUrl = new Uri("https://YOURDOMAIN/api/ivr/handle-call");
            //var actionUrl = new Uri("https://localhost:7218/api/ivr/handle-call");

            //-------------------------------------------------------
            // FIRST CALL
            //-------------------------------------------------------
            // TEMP ONLY
            // Replace localhost with Azure Blob SAS URL after deployment.
            var baseUrl = "https://localhost:7218";

            var actionUrl = new Uri($"{baseUrl}/api/ivr/handle-call");

            var mainMenuUrl = $"{baseUrl}/recordings/MainMenu.mp3";

            if (string.IsNullOrWhiteSpace(digits))
            {
                var gather = new Gather(
                    action: actionUrl,
                    method: "POST",
                    numDigits: 1,
                    timeout: 10);

                //gather.Play(new Uri(_blobStorageService.GenerateSasUrl("MainMenu.mp3")));
                gather.Play(new Uri(mainMenuUrl)); //Temp URL

                response.Append(gather);

                return response;
            }

            //-------------------------------------------------------
            // OPTION 1
            //-------------------------------------------------------
            if (digits == "1")
            {
                var gather = new Gather(
                    action: actionUrl,
                    method: "POST",
                    numDigits: 1,
                    timeout: 10);

                //gather.Play(new Uri(_blobStorageService.GenerateSasUrl("SponsorAllMenu.mp3")));
                gather.Play(new Uri(mainMenuUrl)); //Temp URL

                response.Append(gather);

                return response;
            }

            //-------------------------------------------------------
            // OPTION 2
            //-------------------------------------------------------
            if (digits == "2")
            {
                var gather = new Gather(
                    action: actionUrl,
                    method: "POST",
                    numDigits: 1,
                    timeout: 10);

                //gather.Play(new Uri(_blobStorageService.GenerateSasUrl("SponsorSpecificMenu.mp3")));
                gather.Play(new Uri(mainMenuUrl)); //Temp URL

                response.Append(gather);

                return response;
            }

            //-------------------------------------------------------
            // OPTION 3
            //-------------------------------------------------------
            if (digits == "3")
            {
                var gather = new Gather(
                    action: actionUrl,
                    method: "POST",
                    timeout: 10);

                //gather.Play(new Uri(_blobStorageService.GenerateSasUrl("ContestantList.mp3")));
                gather.Play(new Uri(mainMenuUrl)); //Temp URL

                response.Append(gather);

                return response;
            }

            //-------------------------------------------------------
            // INVALID OPTION
            //-------------------------------------------------------
            {
                var gather = new Gather(
                    action: actionUrl,
                    method: "POST",
                    numDigits: 1,
                    timeout: 10);

                //gather.Play(new Uri(_blobStorageService.GenerateSasUrl("InvalidOption.mp3")));
                gather.Play(new Uri(mainMenuUrl)); //Temp URL

                response.Append(gather);

                return response;
            }
        }
    }
}