using KolHaNitzachon.PhoneSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Persistence
{
    public class PhoneSystemDbContext : DbContext
    {
        public PhoneSystemDbContext(DbContextOptions<PhoneSystemDbContext> options)
            : base(options)
        {
        }

        public DbSet<Recipient> Recipients => Set<Recipient>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PhoneSystemDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}