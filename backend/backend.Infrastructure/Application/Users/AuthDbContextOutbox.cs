using backend.Domain.Data;
using backend.Domain.Models;
using backend.Shared.Application.Messaging;

namespace backend.Infrastructure.Application.Users;

public sealed class AuthDbContextOutbox : IIntegrationEventOutbox
{
    private readonly AuthDbContext _db;

    public AuthDbContextOutbox(AuthDbContext db)
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
