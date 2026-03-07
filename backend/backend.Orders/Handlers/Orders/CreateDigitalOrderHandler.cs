using backend.Application.Results;
using backend.Application.Users;
using backend.Application.Messaging;
using backend.Application.Messaging.Messages;
using backend.Application.Orders;
using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Requests.Orders;
using MediatR;

namespace backend.Handlers.Orders;

public sealed class CreateDigitalOrderHandler : IRequestHandler<CreateDigitalOrderCommand, Result<OrderViewDto>>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;
    private readonly IIntegrationEventOutbox _outbox;

    public CreateDigitalOrderHandler(
        AppDbContext db,
        IEffectiveUserAccessor effectiveUser,
        IIntegrationEventOutbox outbox)
    {
        _db = db;
        _effectiveUser = effectiveUser;
        _outbox = outbox;
    }

    public async Task<Result<OrderViewDto>> Handle(CreateDigitalOrderCommand req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var order = req.ToEntity();
        order.UserId = userId;
        order.DownloadUrl = req.DownloadUrl.Trim();
        order.Status = OrderStatuses.PaymentPending;

        _db.Orders.Add(order);

        await _outbox.EnqueueAsync(
            IntegrationRoutingKeys.OrderPaymentRequested,
            new OrderPaymentRequestedMessage(
                order.Id,
                userId,
                "digital",
                order.TotalAmount,
                DateTime.UtcNow),
            order.Id.ToString(),
            ct);

        await _db.SaveChangesAsync(ct);

        return Result<OrderViewDto>.Success(OrderMapper.ToDto((backend.Models.Order)order));
    }
}
