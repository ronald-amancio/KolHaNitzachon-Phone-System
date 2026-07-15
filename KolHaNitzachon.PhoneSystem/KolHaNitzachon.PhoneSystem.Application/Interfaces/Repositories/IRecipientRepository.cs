using KolHaNitzachon.PhoneSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.Repositories
{
    public interface IRecipientRepository
    {
        Task<IEnumerable<Recipient>> GetAllAsync();

        Task<Recipient?> GetByIdAsync(Guid id);

        Task AddAsync(Recipient recipient);

        Task UpdateAsync(Recipient recipient);

        Task DeleteAsync(Guid id);
    }
}