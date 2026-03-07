using backend.Application.Messaging;
using backend.Data;
using backend.Models;

namespace backend.Infrastructure.Messaging;

public sealed class DbIntegrationEventOutbox : IIntegrationEventOutbox
{
    private readonly AppDbContext _db;

    public DbIntegrationEventOutbox(AppDbContext db)
    {
        _db = db;
    }

    public Task EnqueueAsync<T>(
        string routingKey,
        T message,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        var outboxMessage = new OutboxMessage
        {
            EventType = typeof(T).Name,
            RoutingKey = routingKey,
            Payload = IntegrationEventSerializer.Serialize(message),
            CorrelationId = correlationId,
            OccurredAtUtc = DateTime.UtcNow
        };

        _db.OutboxMessages.Add(outboxMessage);
        return Task.CompletedTask;
    }
}
