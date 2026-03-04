using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Orders;
using MediatR;

namespace backend.Handlers.Orders;

public sealed class CreateDigitalOrderHandler : IRequestHandler<CreateDigitalOrderCommand, OrderViewDto>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public CreateDigitalOrderHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<OrderViewDto> Handle(CreateDigitalOrderCommand req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var order = new DigitalOrder
        {
            UserId = userId,
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
