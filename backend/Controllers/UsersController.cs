using System.Security.Claims;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db)
    {
        await GetOrCreateCurrentUser(db);

        // push mapping to SQL and avoid materializing entity graph at all
        // Benefits:
        // No full entity graph in memory
        // Less RAM
        // Faster
        // No need for Include
        // Still no tracking
        var result = await db.AppUsers
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
            .ToListAsync();

        return Ok(result);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(Guid id, [FromServices] AppDbContext db)
    {
        var user = await db.AppUsers
            .AsNoTracking()
            .Include(x => x.Orders)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null) return NotFound();

        return Ok(MapUser(user));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Subject)) return BadRequest("Subject is required");
        if (string.IsNullOrWhiteSpace(req.Username)) return BadRequest("Username is required");

        var subject = req.Subject.Trim();
        var existing = await db.AppUsers.AnyAsync(x => x.Subject == subject);
        if (existing) return Conflict("User with this subject already exists");

        var user = new AppUser
        {
            Subject = subject,
            Username = req.Username.Trim(),
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim()
        };

        db.AppUsers.Add(user);
        await db.SaveChangesAsync();

        var created = await db.AppUsers
            .AsNoTracking()
            .Include(x => x.Orders)
            .FirstAsync(x => x.Id == user.Id);

        return Ok(MapUser(created));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest req,
        [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Username)) return BadRequest("Username is required");

        var user = await db.AppUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound();

        user.Username = req.Username.Trim();
        user.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();

        await db.SaveChangesAsync();

        var updated = await db.AppUsers
            .AsNoTracking()
            .Include(x => x.Orders)
            .FirstAsync(x => x.Id == id);

        return Ok(MapUser(updated));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromServices] AppDbContext db)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound();

        db.AppUsers.Remove(user);
        await db.SaveChangesAsync();

        return Ok(new { id });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me([FromServices] AppDbContext db)
    {
        var ensured = await GetOrCreateCurrentUser(db);
        if (ensured == null) return Unauthorized("Missing sub");

        var user = await db.AppUsers
            .AsNoTracking()
            .Include(x => x.Orders)
            .FirstAsync(x => x.Id == ensured.Id);

        return Ok(MapUser(user));
    }

    private async Task<AppUser?> GetOrCreateCurrentUser(AppDbContext db)
    {
        var sub = User.Identity?.Name ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(sub)) return null;

        var user = await db.AppUsers.FirstOrDefaultAsync(x => x.Subject == sub);
        if (user != null) return user;

        user = new AppUser
        {
            Subject = sub,
            Username = User.FindFirstValue("preferred_username") ?? $"user-{sub[..8]}",
            Email = User.FindFirstValue("email")
        };

        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static UserWithOrdersDto MapUser(Models.AppUser user) =>
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

    private static OrderViewDto MapOrder(Models.Order order) =>
        order switch
        {
            Models.DigitalOrder digital => new(
                digital.Id,
                "digital",
                digital.TotalAmount,
                digital.Status,
                digital.CreatedAtUtc,
                digital.DownloadUrl,
                null,
                null
            ),
            Models.PhysicalOrder physical => new(
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
