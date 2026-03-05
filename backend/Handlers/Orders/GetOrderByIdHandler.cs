using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Mappers;
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

        return order == null ? null : OrderMapper.ToDto(order);
    }
}
