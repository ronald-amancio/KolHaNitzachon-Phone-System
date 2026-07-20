using KolHaNitzachon.PhoneSystem.Domain.Entities;

namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.Repositories
{
    public interface IRecipientRepository
    {
        Task<IReadOnlyCollection<Recipient>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task<Recipient?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<Recipient?> GetByCodeAsync(
            int code,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Recipient recipient,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Recipient recipient,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
