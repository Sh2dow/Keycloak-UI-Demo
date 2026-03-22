using backend.Domain.Data;
using backend.Domain.Models;
using backend.Shared.Application.Messaging;

namespace backend.Infrastructure.Infrastructure.Messaging;

public sealed class DbIntegrationEventOutbox : IIntegrationEventOutbox
{
    private readonly OrdersDbContext _db;

    public DbIntegrationEventOutbox(OrdersDbContext db)
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
