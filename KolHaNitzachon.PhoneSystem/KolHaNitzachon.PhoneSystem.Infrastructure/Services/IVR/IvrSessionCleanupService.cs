using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Services.IVR
{
    public sealed class IvrSessionCleanupService : BackgroundService
    {
        private static readonly TimeSpan CleanupInterval =
            TimeSpan.FromMinutes(5);

        private readonly IIvrCallSessionStore _sessionStore;

        private readonly ILogger<IvrSessionCleanupService> _logger;

        public IvrSessionCleanupService(IIvrCallSessionStore sessionStore, ILogger<IvrSessionCleanupService> logger)
        {
            _sessionStore = sessionStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(CleanupInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    var removed = _sessionStore.RemoveExpiredSessions();

                    _logger.LogDebug(
                        "IVR session cleanup completed. " +
                        "Removed={RemovedCount}",
                        removed);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Expected while the application is shutting down.
            }
        }
    }
}