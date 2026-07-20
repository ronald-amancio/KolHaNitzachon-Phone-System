using KolHaNitzachon.PhoneSystem.Application.Interfaces.Repositories;
using KolHaNitzachon.PhoneSystem.Domain.Entities;
using KolHaNitzachon.PhoneSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Repositories
{
    /*
     * PRODUCTION EF CORE REPOSITORY
     *
     * Register this implementation in Program.cs when the IVR is ready to
     * use live recipient data:
     *
     * builder.Services.AddScoped<
     *     IRecipientRepository,
     *     RecipientRepository>();
     */
    public class RecipientRepository : IRecipientRepository
    {
        private readonly PhoneSystemDbContext _context;

        public RecipientRepository(PhoneSystemDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<Recipient>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.Recipients
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<Recipient?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Recipients
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);
        }

        public async Task<Recipient?> GetByCodeAsync(
            int code,
            CancellationToken cancellationToken = default)
        {
            return await _context.Recipients
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Code == code,
                    cancellationToken);
        }

        public async Task AddAsync(
            Recipient recipient,
            CancellationToken cancellationToken = default)
        {
            await _context.Recipients.AddAsync(
                recipient,
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(
            Recipient recipient,
            CancellationToken cancellationToken = default)
        {
            _context.Recipients.Update(recipient);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var recipient = await _context.Recipients
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (recipient is null)
            {
                return;
            }

            _context.Recipients.Remove(recipient);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
