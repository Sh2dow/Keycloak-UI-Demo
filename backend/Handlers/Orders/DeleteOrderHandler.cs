using backend.Application.Users;
using backend.Data;
using backend.Requests.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Orders;

public sealed class DeleteOrderHandler : IRequestHandler<DeleteOrderCommand, bool>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public DeleteOrderHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<bool> Handle(DeleteOrderCommand req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var order = await _db.Orders
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.UserId == userId, ct);
        if (order == null) return false;

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
