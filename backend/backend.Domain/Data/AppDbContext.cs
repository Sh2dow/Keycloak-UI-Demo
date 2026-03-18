using backend.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domain.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderSagaState> OrderSagaStates => Set<OrderSagaState>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ConsumedMessage> ConsumedMessages => Set<ConsumedMessage>();
    public DbSet<PaymentEventRecord> PaymentEventRecords => Set<PaymentEventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(x => x.Priority)
                .IsRequired()
                .HasMaxLength(32);

            entity.HasIndex(x => x.UserId);

            entity.HasMany(x => x.Comments)
                .WithOne(x => x.Task)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Content)
                .IsRequired()
                .HasMaxLength(1000);

            entity.HasIndex(x => x.TaskId);
            entity.HasIndex(x => x.AuthorId);
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

        modelBuilder.Entity<Order>().Ignore(x => x.Events);

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

        modelBuilder.Entity<OrderSagaState>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.State)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(x => x.ExecutionFailureReason)
                .HasMaxLength(1000);

            entity.HasIndex(x => x.OrderId)
                .IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.EventType)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.RoutingKey)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Payload)
                .IsRequired();

            entity.Property(x => x.CorrelationId)
                .HasMaxLength(100);

            entity.Property(x => x.LastError)
                .HasMaxLength(2000);

            entity.HasIndex(x => x.PublishedAtUtc);
            entity.HasIndex(x => x.OccurredAtUtc);
        });

        modelBuilder.Entity<ConsumedMessage>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Consumer)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.MessageId)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(x => new { x.Consumer, x.MessageId })
                .IsUnique();
        });

    modelBuilder.Entity<PaymentEventRecord>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.EventType)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Data)
                .IsRequired();

            entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => new { x.OrderId, x.AttemptNumber, x.SequenceNumber })
                .IsUnique();
            entity.HasIndex(x => new { x.PaymentId, x.SequenceNumber })
                .IsUnique();
        });
    }
}
