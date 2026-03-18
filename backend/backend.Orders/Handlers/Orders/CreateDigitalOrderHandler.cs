using backend.Application.Messaging;
using backend.Application.Messaging.Messages;
using backend.Application.Orders;
using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Requests.Orders;
using backend.Validation.Orders;
using MediatR;

namespace backend.Handlers.Orders;

public sealed class CreateDigitalOrderHandler : IRequestHandler<CreateDigitalOrderCommand, backend.Application.Results.Result<OrderViewDto>>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;
    private readonly IIntegrationEventOutbox _outbox;
    private readonly CreateDigitalOrderCommandValidator _validator;

    public CreateDigitalOrderHandler(
        AppDbContext db,
        IEffectiveUserAccessor effectiveUser,
        IIntegrationEventOutbox outbox,
        CreateDigitalOrderCommandValidator validator)
    {
        _db = db;
        _effectiveUser = effectiveUser;
        _outbox = outbox;
        _validator = validator;
    }

    public async Task<backend.Application.Results.Result<OrderViewDto>> Handle(CreateDigitalOrderCommand req, CancellationToken ct)
    {
        // Command validation
        var commandResult = _validator.ValidateCommand(req);
        if (!commandResult.IsSuccess)
        {
            return backend.Application.Results.Result<OrderViewDto>.ValidationFromDomainErrors(commandResult.Errors);
        }

        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var order = req.ToEntity();
        order.UserId = userId;
        order.TotalAmount = req.TotalAmount;
        order.DownloadUrl = req.DownloadUrl.Trim();
        order.Status = OrderStatuses.PaymentPending;

            // Domain-level validation
        var domainResult = order.ValidateDigitalOrder();
        if (!domainResult.IsSuccess)
        {
            return backend.Application.Results.Result<OrderViewDto>.ValidationFromDomainErrors(domainResult.Errors);
        }

        _db.Orders.Add(order);

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

         return backend.Application.Results.Result<OrderViewDto>.Success(OrderMapper.ToDto((backend.Models.Order)order));
    }
}
