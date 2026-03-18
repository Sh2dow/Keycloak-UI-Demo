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

public sealed class CreatePhysicalOrderHandler : IRequestHandler<CreatePhysicalOrderCommand, backend.Application.Results.Result<OrderViewDto>>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;
    private readonly IIntegrationEventOutbox _outbox;
    private readonly CreatePhysicalOrderCommandValidator _validator;

    public CreatePhysicalOrderHandler(
        AppDbContext db,
        IEffectiveUserAccessor effectiveUser,
        IIntegrationEventOutbox outbox,
        CreatePhysicalOrderCommandValidator validator)
    {
        _db = db;
        _effectiveUser = effectiveUser;
        _outbox = outbox;
        _validator = validator;
    }

    public async Task<backend.Application.Results.Result<OrderViewDto>> Handle(CreatePhysicalOrderCommand req, CancellationToken ct)
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
        order.ShippingAddress = req.ShippingAddress.Trim();
        order.TrackingNumber = string.IsNullOrWhiteSpace(req.TrackingNumber) ? null : req.TrackingNumber.Trim();
        order.Status = OrderStatuses.PaymentPending;

         // Domain-level validation
        var domainResult = order.ValidatePhysicalOrder();
        if (!domainResult.IsSuccess)
        {
            return backend.Application.Results.Result<OrderViewDto>.ValidationFromDomainErrors(domainResult.Errors);
        }

        _db.Orders.Add(order);

        await _outbox.EnqueueAsync(
            IntegrationRoutingKeys.OrderPaymentRequested,
            new OrderPaymentRequestedMessage(
                order.Id,
                userId,
                "physical",
                order.TotalAmount,
                DateTime.UtcNow),
            order.Id.ToString(),
            ct);

        await _db.SaveChangesAsync(ct);

        return backend.Application.Results.Result<OrderViewDto>.Success(OrderMapper.ToDto((backend.Models.Order)order));
    }
}
