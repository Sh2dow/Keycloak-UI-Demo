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

    public GetOrdersHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OrderViewDto>> Handle(GetOrdersQuery req, CancellationToken ct)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(x => x.UserId == req.UserId)
            .OrderByDescending(x => x.CreatedAtUtc)
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
