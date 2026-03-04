using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Orders;

public sealed class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderViewDto?>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public GetOrderByIdHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<OrderViewDto?> Handle(GetOrderByIdQuery req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.UserId == userId, ct);

        return order == null ? null : MapOrder(order);
    }

    private static OrderViewDto MapOrder(Order order) =>
        order switch
        {
            DigitalOrder digital => new(
                digital.Id,
                "digital",
                digital.TotalAmount,
                digital.Status,
                digital.CreatedAtUtc,
                digital.DownloadUrl,
                null,
                null
            ),
            PhysicalOrder physical => new(
                physical.Id,
                "physical",
                physical.TotalAmount,
                physical.Status,
                physical.CreatedAtUtc,
                null,
                physical.ShippingAddress,
                physical.TrackingNumber
            ),
            _ => new(
                order.Id,
                "unknown",
                order.TotalAmount,
                order.Status,
                order.CreatedAtUtc,
                null,
                null,
                null
            )
        };
}
