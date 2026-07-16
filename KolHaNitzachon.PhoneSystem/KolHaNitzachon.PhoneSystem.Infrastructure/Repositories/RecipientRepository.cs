using KolHaNitzachon.PhoneSystem.Application.Interfaces.Repositories;
using KolHaNitzachon.PhoneSystem.Domain.Entities;
using KolHaNitzachon.PhoneSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Repositories
{
    public class RecipientRepository : IRecipientRepository
    {
        private readonly PhoneSystemDbContext _context;

        public RecipientRepository(PhoneSystemDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Recipient>> GetAllAsync()
        {
            return await _context.Recipients.ToListAsync();
        }

        public async Task<Recipient?> GetByIdAsync(Guid id)
        {
            return await _context.Recipients.FindAsync(id);
        }

        public async Task AddAsync(Recipient recipient)
        {
            _context.Recipients.Add(recipient);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Recipient recipient)
        {
            _context.Recipients.Update(recipient);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var recipient = await _context.Recipients.FindAsync(id);

            if (recipient == null)
                return;

            _context.Recipients.Remove(recipient);

            await _context.SaveChangesAsync();
        }
    }
}
