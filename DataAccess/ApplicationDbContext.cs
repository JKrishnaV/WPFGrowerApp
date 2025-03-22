using Microsoft.EntityFrameworkCore;
using WPFGrowerApp.Models.Entities;

namespace WPFGrowerApp.DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<GrowerEntity> Growers { get; set; }
        public DbSet<AccountEntity> Accounts { get; set; }
        public DbSet<ChequeEntity> Cheques { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite key for ChequeEntity
            modelBuilder.Entity<ChequeEntity>()
                .HasKey(c => new { c.Series, c.Cheque });

            // Configure any additional model constraints or relationships here
        }
    }
}
