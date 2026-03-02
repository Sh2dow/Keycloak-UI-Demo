using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.UserSub)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasIndex(x => x.UserSub);
        });

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

            entity.HasMany(x => x.Orders)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(x => x.UserId);

            entity.HasDiscriminator<string>("order_type")
                .HasValue<DigitalOrder>("digital")
                .HasValue<PhysicalOrder>("physical");
        });

        modelBuilder.Entity<DigitalOrder>(entity =>
        {
            entity.Property(x => x.DownloadUrl)
                .IsRequired()
                .HasMaxLength(500);
        });

        modelBuilder.Entity<PhysicalOrder>(entity =>
        {
            entity.Property(x => x.ShippingAddress)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(x => x.TrackingNumber)
                .HasMaxLength(100);
        });
    }
}
