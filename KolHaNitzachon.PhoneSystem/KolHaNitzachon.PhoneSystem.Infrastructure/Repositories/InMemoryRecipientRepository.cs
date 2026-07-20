using KolHaNitzachon.PhoneSystem.Application.Interfaces.Repositories;
using KolHaNitzachon.PhoneSystem.Domain.Entities;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Repositories
{
    /*
     * =====================================================================
     * TEMPORARY IN-MEMORY REPOSITORY
     * =====================================================================
     *
     * This class is optional while IVRFlowController directly uses its
     * clearly marked TestRecipients collection.
     *
     * It is kept here so the repository architecture is ready for local
     * integration testing without SQL Server.
     *
     * Program.cs local registration:
     *
     * builder.Services.AddSingleton<
     *     IRecipientRepository,
     *     InMemoryRecipientRepository>();
     *
     * Production registration:
     *
     * builder.Services.AddScoped<
     *     IRecipientRepository,
     *     RecipientRepository>();
     *
     * =====================================================================
     */
    public class InMemoryRecipientRepository : IRecipientRepository
    {
        private static readonly IReadOnlyCollection<Recipient> Recipients =
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

        public Task<IReadOnlyCollection<Recipient>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Recipients);
        }

        public Task<Recipient?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var recipient = Recipients.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(recipient);
        }

        public Task<Recipient?> GetByCodeAsync(
            int code,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var recipient = Recipients.FirstOrDefault(x => x.Code == code);
            return Task.FromResult(recipient);
        }

        public Task AddAsync(
            Recipient recipient,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "The temporary in-memory repository is read-only.");
        }

        public Task UpdateAsync(
            Recipient recipient,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "The temporary in-memory repository is read-only.");
        }

        public Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "The temporary in-memory repository is read-only.");
        }
    }
}
