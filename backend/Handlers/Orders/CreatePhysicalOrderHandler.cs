using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Orders;
using MediatR;

namespace backend.Handlers.Orders;

public sealed class CreatePhysicalOrderHandler : IRequestHandler<CreatePhysicalOrderCommand, OrderViewDto>
{
    private readonly AppDbContext _db;

    public CreatePhysicalOrderHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrderViewDto> Handle(CreatePhysicalOrderCommand req, CancellationToken ct)
    {
        var order = new PhysicalOrder
        {
            UserId = req.UserId,
            TotalAmount = req.TotalAmount,
            ShippingAddress = req.ShippingAddress.Trim(),
            TrackingNumber = string.IsNullOrWhiteSpace(req.TrackingNumber) ? null : req.TrackingNumber.Trim(),
            Status = "Created"
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        return new OrderViewDto(
            order.Id,
            "physical",
            order.TotalAmount,
            order.Status,
            order.CreatedAtUtc,
            null,
            order.ShippingAddress,
            order.TrackingNumber
        );
    }
}
