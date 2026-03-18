using System;
using System.Threading;
using System.Threading.Tasks;
using backend.Domain.Data;
using backend.Domain.Models;
using backend.Orders.Application.Orders;
using backend.Orders.Dtos;
using backend.Orders.Mappers;
using backend.Orders.Requests.Orders;
using backend.Orders.Validation.Orders;
using backend.Shared.Application.Messaging;
using backend.Shared.Application.Messaging.Messages;
using backend.Shared.Application.Users;
using MediatR;

namespace backend.Orders.Handlers.Orders;

public sealed class CreateDigitalOrderHandler : IRequestHandler<CreateDigitalOrderCommand, Shared.Application.Results.Result<OrderViewDto>>
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

    public async Task<Shared.Application.Results.Result<OrderViewDto>> Handle(CreateDigitalOrderCommand req, CancellationToken ct)
    {
        // Command validation
        var commandResult = _validator.ValidateCommand(req);
        if (!commandResult.IsSuccess)
        {
            return Shared.Application.Results.Result<OrderViewDto>.ValidationFromDomainErrors(commandResult.Errors);
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
            return Shared.Application.Results.Result<OrderViewDto>.ValidationFromDomainErrors(domainResult.Errors);
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

         return Shared.Application.Results.Result<OrderViewDto>.Success(OrderMapper.ToDto((Order)order));
    }
}
