using System.Security.Claims;
using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    public sealed record CreateUserRequest(string Subject, string Username, string? Email);
    public sealed record UpdateUserRequest(string Username, string? Email);

    public sealed record OrderViewDto(
        Guid Id,
        string OrderType,
        decimal TotalAmount,
        string Status,
        DateTime CreatedAtUtc,
        string? DownloadUrl,
        string? ShippingAddress,
        string? TrackingNumber
    );

    public sealed record UserWithOrdersDto(
        Guid Id,
        string Subject,
        string Username,
        string? Email,
        DateTime CreatedAtUtc,
        IReadOnlyList<OrderViewDto> Orders
    );

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db)
    {
        await GetOrCreateCurrentUser(db);

        var users = await db.AppUsers
            .AsNoTracking()
            .Include(x => x.Orders)
            .OrderBy(x => x.Username)
            .ToListAsync();

        var result = users.Select(MapUser).ToList();
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
