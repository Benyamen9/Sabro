using Microsoft.EntityFrameworkCore;
using Sabro.Data.Entities;

namespace Sabro.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the User entity
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
            });

            // Seed data
            modelBuilder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                });
        }
    }
}
