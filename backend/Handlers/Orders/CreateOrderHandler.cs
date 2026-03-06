using backend.Application.Results;
using backend.Application.Users;
using backend.Dtos;
using backend.Requests.Orders;
using MediatR;

namespace backend.Handlers.Orders;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<OrderViewDto>>
{
    private readonly IMediator _mediator;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public CreateOrderHandler(IMediator mediator, IEffectiveUserAccessor effectiveUser)
    {
        _mediator = mediator;
        _effectiveUser = effectiveUser;
    }

    public async Task<Result<OrderViewDto>> Handle(CreateOrderCommand req, CancellationToken ct)
    {
        await _effectiveUser.GetUserIdAsync(ct);
        var orderType = req.OrderType.Trim().ToLowerInvariant();

        return orderType switch
        {
            "digital" => await _mediator.Send(
                new CreateDigitalOrderCommand(req.TotalAmount, req.DownloadUrl!),
                ct
            ),
            "physical" => await _mediator.Send(
                new CreatePhysicalOrderCommand(req.TotalAmount, req.ShippingAddress!, req.TrackingNumber),
                ct
            ),
            _ => Result<OrderViewDto>.Validation([new ResultError("validation", "OrderType must be either 'digital' or 'physical'.", nameof(req.OrderType))])
        };
    }
}
