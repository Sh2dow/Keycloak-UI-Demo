using backend.Application.Messaging;
using backend.Application.Messaging.Messages;
using backend.Application.Orders;
using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Models;
using backend.Requests.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Orders;

public sealed class RetryOrderPaymentHandler : IRequestHandler<RetryOrderPaymentCommand, backend.Application.Results.Result<OrderViewDto>>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;
    private readonly IIntegrationEventOutbox _outbox;

    public RetryOrderPaymentHandler(
        AppDbContext db,
        IEffectiveUserAccessor effectiveUser,
        IIntegrationEventOutbox outbox)
    {
        _db = db;
        _effectiveUser = effectiveUser;
        _outbox = outbox;
    }

    public async Task<backend.Application.Results.Result<OrderViewDto>> Handle(RetryOrderPaymentCommand req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == req.OrderId && x.UserId == userId, ct);
        if (order == null)
        {
            return backend.Application.Results.Result<OrderViewDto>.NotFound();
        }

        if (!string.Equals(order.Status, OrderStatuses.PaymentFailed, StringComparison.OrdinalIgnoreCase))
        {
            return backend.Application.Results.Result<OrderViewDto>.Conflict("Only orders with failed payments can be retried.");
        }

        var requestedAtUtc = DateTime.UtcNow;
        order.Status = OrderStatuses.PaymentPending;

        var saga = await _db.OrderSagaStates.FirstOrDefaultAsync(x => x.OrderId == order.Id, ct);
        if (saga == null)
        {
            saga = new OrderSagaState
            {
                OrderId = order.Id,
                State = OrderSagaStates.PaymentPending,
                LastPaymentRequestedAtUtc = requestedAtUtc,
                UpdatedAtUtc = requestedAtUtc,
                Version = 1
            };

            _db.OrderSagaStates.Add(saga);
        }
        else
        {
            saga.State = OrderSagaStates.PaymentPending;
            saga.PaymentId = null;
            saga.LastPaymentRequestedAtUtc = requestedAtUtc;
            saga.LastPaymentCompletedAtUtc = null;
            saga.ExecutionDispatchedAtUtc = null;
            saga.ExecutionStartedAtUtc = null;
            saga.ExecutionCompletedAtUtc = null;
            saga.ExecutionFailedAtUtc = null;
            saga.ExecutionFailureReason = null;
            saga.UpdatedAtUtc = requestedAtUtc;
            saga.Version += 1;
        }

        await _outbox.EnqueueAsync(
            IntegrationRoutingKeys.OrderPaymentRequested,
            new OrderPaymentRequestedMessage(
                order.Id,
                userId,
                order switch
                {
                    DigitalOrder => "digital",
                    PhysicalOrder => "physical",
                    _ => "unknown"
                },
                order.TotalAmount,
                requestedAtUtc),
            order.Id.ToString(),
            ct);

        await _db.SaveChangesAsync(ct);

        return backend.Application.Results.Result<OrderViewDto>.Success(OrderMapper.ToDto(order));
    }
}
