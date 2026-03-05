using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Mappers;
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
        var order = req.ToEntity();
        order.UserId = userId;
        order.DownloadUrl = req.DownloadUrl.Trim();
        order.Status = "Created";

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        return OrderMapper.ToDto((backend.Models.Order)order);
    }
}
