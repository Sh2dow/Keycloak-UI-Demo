using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Users;

public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UserWithOrdersDto?>
{
    private readonly AppDbContext _db;

    public UpdateUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserWithOrdersDto?> Handle(UpdateUserCommand req, CancellationToken ct)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (user == null) return null;

        user.Username = req.Username.Trim();
        user.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
        await _db.SaveChangesAsync(ct);

        var updated = await _db.AppUsers
            .AsNoTracking()
            .Include(x => x.Orders)
            .FirstAsync(x => x.Id == req.Id, ct);

        return MapUser(updated);
    }

    private static UserWithOrdersDto MapUser(AppUser user) =>
        new(
            user.Id,
            user.Subject,
            user.Username,
            user.Email,
            user.CreatedAtUtc,
            user.Orders
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(MapOrder)
                .ToList()
        );

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
