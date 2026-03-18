using backend.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domain.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Subject)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(x => x.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Email)
                .HasMaxLength(200);

            entity.HasIndex(x => x.Subject)
                .IsUnique();
        });
    }
}
