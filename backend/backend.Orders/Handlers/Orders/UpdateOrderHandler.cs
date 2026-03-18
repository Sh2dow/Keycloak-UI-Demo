using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Mappers;
using backend.Requests.Orders;
using backend.Validation.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultError = backend.Application.Results.ResultError;

namespace backend.Handlers.Orders;

public sealed class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, backend.Application.Results.Result<OrderViewDto>>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;
    private readonly UpdateOrderCommandValidator _validator;

    public UpdateOrderHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser, UpdateOrderCommandValidator validator)
    {
        _db = db;
        _effectiveUser = effectiveUser;
        _validator = validator;
    }

    public async Task<backend.Application.Results.Result<OrderViewDto>> Handle(UpdateOrderCommand req, CancellationToken ct)
    {
           // Command validation
        var commandResult = _validator.ValidateCommand(req);
        if (!commandResult.IsSuccess)
        {
            return backend.Application.Results.Result<OrderViewDto>.ValidationFromDomainErrors(commandResult.Errors);
        }

        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var order = await _db.Orders
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.UserId == userId, ct);
        if (order == null) return backend.Application.Results.Result<OrderViewDto>.NotFound("Order not found.");

        order.TotalAmount = req.TotalAmount;
        order.Status = req.Status.Trim();

        switch (order)
        {
            case DigitalOrder digital:
                if (string.IsNullOrWhiteSpace(req.DownloadUrl))
                {
                    return backend.Application.Results.Result<OrderViewDto>.Validation([new ResultError("validation", "DownloadUrl is required for digital orders.", nameof(req.DownloadUrl))]);
                }

                digital.DownloadUrl = req.DownloadUrl.Trim();
                break;
            case PhysicalOrder physical:
                if (string.IsNullOrWhiteSpace(req.ShippingAddress))
                {
                    return backend.Application.Results.Result<OrderViewDto>.Validation([new ResultError("validation", "ShippingAddress is required for physical orders.", nameof(req.ShippingAddress))]);
                }

                physical.ShippingAddress = req.ShippingAddress.Trim();
                physical.TrackingNumber = string.IsNullOrWhiteSpace(req.TrackingNumber) ? null : req.TrackingNumber.Trim();
                break;
        }

        // Domain-level validation
        DomainResult<DomainUnit> domainResult;
        switch (order)
        {
            case DigitalOrder digital:
                domainResult = order.ValidateDigitalOrder();
                break;
            case PhysicalOrder physical:
                domainResult = order.ValidatePhysicalOrder();
                break;
            default:
                domainResult = order.Validate();
                break;
        }
        
           if (!domainResult.IsSuccess)
        {
            return backend.Application.Results.Result<OrderViewDto>.ValidationFromDomainErrors(domainResult.Errors);
        }

        await _db.SaveChangesAsync(ct);

        return backend.Application.Results.Result<OrderViewDto>.Success(OrderMapper.ToDto(order));
    }
}


