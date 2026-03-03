using backend.Data;
using backend.Requests.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Orders;

public sealed class DeleteOrderHandler : IRequestHandler<DeleteOrderCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteOrderHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(DeleteOrderCommand req, CancellationToken ct)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.UserId == req.UserId, ct);
        if (order == null) return false;

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
