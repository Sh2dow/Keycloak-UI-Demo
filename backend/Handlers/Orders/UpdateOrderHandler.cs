using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Models;
using backend.Requests.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Orders;

public sealed class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, UpdateOrderResult>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public UpdateOrderHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<UpdateOrderResult> Handle(UpdateOrderCommand req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var order = await _db.Orders
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.UserId == userId, ct);
        if (order == null) return new UpdateOrderResult(true, null, null);

        order.TotalAmount = req.TotalAmount;
        order.Status = req.Status.Trim();

        switch (order)
        {
            case DigitalOrder digital:
                if (string.IsNullOrWhiteSpace(req.DownloadUrl))
                {
                    return new UpdateOrderResult(false, "DownloadUrl is required for digital orders", null);
                }

                digital.DownloadUrl = req.DownloadUrl.Trim();
                break;
            case PhysicalOrder physical:
                if (string.IsNullOrWhiteSpace(req.ShippingAddress))
                {
                    return new UpdateOrderResult(false, "ShippingAddress is required for physical orders", null);
                }

                physical.ShippingAddress = req.ShippingAddress.Trim();
                physical.TrackingNumber = string.IsNullOrWhiteSpace(req.TrackingNumber) ? null : req.TrackingNumber.Trim();
                break;
        }

        await _db.SaveChangesAsync(ct);

        return new UpdateOrderResult(false, null, OrderMapper.ToDto(order));
    }
}
