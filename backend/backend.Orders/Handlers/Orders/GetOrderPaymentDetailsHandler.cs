using backend.Application.Messaging;
using backend.Application.Messaging.Messages;
using backend.Application.Orders;
using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Orders;

public sealed class GetOrderPaymentDetailsHandler : IRequestHandler<GetOrderPaymentDetailsQuery, OrderPaymentDetailsDto?>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public GetOrderPaymentDetailsHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<OrderPaymentDetailsDto?> Handle(GetOrderPaymentDetailsQuery req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.OrderId && x.UserId == userId, ct);

        if (order == null)
        {
            return null;
        }

        var saga = await _db.OrderSagaStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == req.OrderId, ct);

        var paymentEvents = await _db.PaymentEventRecords
            .AsNoTracking()
            .Where(x => x.OrderId == req.OrderId)
            .OrderBy(x => x.SequenceNumber)
            .ToListAsync(ct);

        var failureReason = paymentEvents
            .Where(x => string.Equals(x.EventType, nameof(PaymentFailedMessage), StringComparison.Ordinal))
            .Select(x => TryGetFailureReason(x.Data))
            .LastOrDefault(x => !string.IsNullOrWhiteSpace(x));

        var paymentState = ResolvePaymentState(order.Status, saga?.State, paymentEvents);
        var currentAttemptNumber = paymentEvents
            .OrderByDescending(x => x.AttemptNumber)
            .Select(x => x.AttemptNumber)
            .FirstOrDefault();

        return new OrderPaymentDetailsDto(
            order.Id,
            saga?.PaymentId ?? paymentEvents.LastOrDefault()?.PaymentId,
            currentAttemptNumber,
            order.Status,
            saga?.State ?? OrderSagaStates.PaymentPending,
            paymentState,
            order.CreatedAtUtc,
            saga?.LastPaymentRequestedAtUtc,
            saga?.LastPaymentCompletedAtUtc,
            saga?.ExecutionDispatchedAtUtc,
            saga?.ExecutionStartedAtUtc,
            saga?.ExecutionCompletedAtUtc,
            saga?.ExecutionFailedAtUtc,
            saga?.ExecutionFailureReason,
            failureReason,
            paymentEvents.Select(MapPaymentEvent).ToArray()
        );
    }

    private static OrderPaymentEventDto MapPaymentEvent(PaymentEventRecord record)
    {
        return record.EventType switch
        {
            nameof(PaymentInitiatedMessage) => new OrderPaymentEventDto(
                record.AttemptNumber,
                record.SequenceNumber,
                record.EventType,
                record.OccurredAtUtc,
                "Payment workflow started.",
                null),
            nameof(PaymentAuthorizedMessage) => new OrderPaymentEventDto(
                record.AttemptNumber,
                record.SequenceNumber,
                record.EventType,
                record.OccurredAtUtc,
                "Payment was authorized.",
                null),
            nameof(PaymentFailedMessage) => new OrderPaymentEventDto(
                record.AttemptNumber,
                record.SequenceNumber,
                record.EventType,
                record.OccurredAtUtc,
                "Payment failed.",
                TryGetFailureReason(record.Data)),
            _ => new OrderPaymentEventDto(
                record.AttemptNumber,
                record.SequenceNumber,
                record.EventType,
                record.OccurredAtUtc,
                "Payment event recorded.",
                null)
        };
    }

    private static string ResolvePaymentState(string orderStatus, string? sagaState, IReadOnlyCollection<PaymentEventRecord> paymentEvents)
    {
        if (string.Equals(orderStatus, OrderStatuses.PaymentFailed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sagaState, OrderSagaStates.PaymentFailed, StringComparison.OrdinalIgnoreCase))
        {
            return OrderSagaStates.PaymentFailed;
        }

        if (string.Equals(orderStatus, OrderStatuses.ExecutionDispatched, StringComparison.OrdinalIgnoreCase)
            || string.Equals(orderStatus, OrderStatuses.ExecutionStarted, StringComparison.OrdinalIgnoreCase)
            || string.Equals(orderStatus, OrderStatuses.ExecutionCompleted, StringComparison.OrdinalIgnoreCase)
            || string.Equals(orderStatus, OrderStatuses.ExecutionFailed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(orderStatus, OrderStatuses.PaymentAuthorized, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sagaState, OrderSagaStates.ExecutionDispatched, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sagaState, OrderSagaStates.ExecutionStarted, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sagaState, OrderSagaStates.ExecutionCompleted, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sagaState, OrderSagaStates.ExecutionFailed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sagaState, OrderSagaStates.PaymentAuthorized, StringComparison.OrdinalIgnoreCase)
            || paymentEvents.Any(x => string.Equals(x.EventType, nameof(PaymentAuthorizedMessage), StringComparison.Ordinal)))
        {
            return OrderSagaStates.PaymentAuthorized;
        }

        if (paymentEvents.Any(x => string.Equals(x.EventType, nameof(PaymentInitiatedMessage), StringComparison.Ordinal)))
        {
            return nameof(PaymentInitiatedMessage);
        }

        return OrderSagaStates.PaymentPending;
    }

    private static string? TryGetFailureReason(string payload)
    {
        try
        {
            return IntegrationEventSerializer.Deserialize<PaymentFailedMessage>(payload).Reason;
        }
        catch
        {
            return null;
        }
    }
}
