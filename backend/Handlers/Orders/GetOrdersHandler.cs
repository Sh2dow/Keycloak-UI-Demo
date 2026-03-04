using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Orders;

public sealed class GetOrdersHandler : IRequestHandler<GetOrdersQuery, IReadOnlyList<OrderViewDto>>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public GetOrdersHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<IReadOnlyList<OrderViewDto>> Handle(GetOrdersQuery req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var pageNumber = Math.Max(req.PageNumber, 1);
        var pageSize = Math.Clamp(req.PageSize, 1, 100);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return orders.Select(MapOrder).ToList();
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
