using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Users;

public sealed class GetUsersHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserWithOrdersDto>>
{
    private readonly AppDbContext _db;

    public GetUsersHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserWithOrdersDto>> Handle(GetUsersQuery req, CancellationToken ct)
    {
        var result = await _db.AppUsers
            .AsNoTracking()
            .OrderBy(x => x.Username)
            .Select(x => new UserWithOrdersDto(
                x.Id,
                x.Subject,
                x.Username,
                x.Email,
                x.CreatedAtUtc,
                x.Orders
                    .OrderByDescending(o => o.CreatedAtUtc)
                    .Select(o => new OrderViewDto(
                        o.Id,
                        o is DigitalOrder ? "digital" : o is PhysicalOrder ? "physical" : "unknown",
                        o.TotalAmount,
                        o.Status,
                        o.CreatedAtUtc,
                        o is DigitalOrder ? ((DigitalOrder)o).DownloadUrl : null,
                        o is PhysicalOrder ? ((PhysicalOrder)o).ShippingAddress : null,
                        o is PhysicalOrder ? ((PhysicalOrder)o).TrackingNumber : null
                    ))
                    .ToList()
            ))
            .ToListAsync(ct);

        return result;
    }
}
