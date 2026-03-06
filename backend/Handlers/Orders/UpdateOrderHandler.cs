using backend.Application.Results;
using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Models;
using backend.Requests.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Orders;

public sealed class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, Result<OrderViewDto>>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public UpdateOrderHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<Result<OrderViewDto>> Handle(UpdateOrderCommand req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var order = await _db.Orders
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.UserId == userId, ct);
        if (order == null) return Result<OrderViewDto>.NotFound("Order not found.");

        order.TotalAmount = req.TotalAmount;
        order.Status = req.Status.Trim();

        switch (order)
        {
            case DigitalOrder digital:
                if (string.IsNullOrWhiteSpace(req.DownloadUrl))
                {
                    return Result<OrderViewDto>.Validation([new ResultError("validation", "DownloadUrl is required for digital orders.", nameof(req.DownloadUrl))]);
                }

                digital.DownloadUrl = req.DownloadUrl.Trim();
                break;
            case PhysicalOrder physical:
                if (string.IsNullOrWhiteSpace(req.ShippingAddress))
                {
                    return Result<OrderViewDto>.Validation([new ResultError("validation", "ShippingAddress is required for physical orders.", nameof(req.ShippingAddress))]);
                }

                physical.ShippingAddress = req.ShippingAddress.Trim();
                physical.TrackingNumber = string.IsNullOrWhiteSpace(req.TrackingNumber) ? null : req.TrackingNumber.Trim();
                break;
        }

        await _db.SaveChangesAsync(ct);

        return Result<OrderViewDto>.Success(OrderMapper.ToDto(order));
    }
}
