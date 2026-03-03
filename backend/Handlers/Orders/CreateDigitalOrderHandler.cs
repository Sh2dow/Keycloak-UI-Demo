using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Orders;
using MediatR;

namespace backend.Handlers.Orders;

public sealed class CreateDigitalOrderHandler : IRequestHandler<CreateDigitalOrderCommand, OrderViewDto>
{
    private readonly AppDbContext _db;

    public CreateDigitalOrderHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrderViewDto> Handle(CreateDigitalOrderCommand req, CancellationToken ct)
    {
        var order = new DigitalOrder
        {
            UserId = req.UserId,
            TotalAmount = req.TotalAmount,
            DownloadUrl = req.DownloadUrl.Trim(),
            Status = "Created"
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        return new OrderViewDto(
            order.Id,
            "digital",
            order.TotalAmount,
            order.Status,
            order.CreatedAtUtc,
            order.DownloadUrl,
            null,
            null
        );
    }
}
