using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Users;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly AppDbContext _db;

    public CreateUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CreateUserResult> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var subject = req.Subject.Trim();
        var exists = await _db.AppUsers.AnyAsync(x => x.Subject == subject, ct);
        if (exists) return new CreateUserResult(true, null);

        var user = new AppUser
        {
            Subject = subject,
            Username = req.Username.Trim(),
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim()
        };

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync(ct);

        var created = await _db.AppUsers
            .AsNoTracking()
            .Include(x => x.Orders)
            .FirstAsync(x => x.Id == user.Id, ct);

        return new CreateUserResult(false, MapUser(created));
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
