using System.Security.Claims;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db, [FromQuery] Guid? asUserId = null)
    {
        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var orders = await db.Orders
            .AsNoTracking()
            .Where(x => x.UserId == user!.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        return Ok(orders.Select(MapOrder).ToList());
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(Guid id, [FromServices] AppDbContext db, [FromQuery] Guid? asUserId = null)
    {
        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var order = await db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user!.Id);

        if (order == null) return NotFound();

        return Ok(MapOrder(order));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest req,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null)
    {
        var orderType = req.OrderType.Trim().ToLowerInvariant();

        if (orderType == "digital")
        {
            return await CreateDigital(new CreateDigitalOrderRequest(req.TotalAmount, req.DownloadUrl ?? string.Empty), db, asUserId);
        }

        if (orderType == "physical")
        {
            return await CreatePhysical(new CreatePhysicalOrderRequest(req.TotalAmount, req.ShippingAddress ?? string.Empty, req.TrackingNumber), db, asUserId);
        }

        return BadRequest("OrderType must be either 'digital' or 'physical'");
    }

    [Authorize]
    [HttpPost("digital")]
    public async Task<IActionResult> CreateDigital(
        [FromBody] CreateDigitalOrderRequest req,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null)
    {
        if (req.TotalAmount <= 0) return BadRequest("TotalAmount must be greater than zero");
        if (string.IsNullOrWhiteSpace(req.DownloadUrl)) return BadRequest("DownloadUrl is required");

        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var order = new DigitalOrder
        {
            UserId = user!.Id,
            TotalAmount = req.TotalAmount,
            DownloadUrl = req.DownloadUrl.Trim(),
            Status = "Created"
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return Ok(MapOrder(order));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateOrderRequest req,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null)
    {
        if (req.TotalAmount <= 0) return BadRequest("TotalAmount must be greater than zero");
        if (string.IsNullOrWhiteSpace(req.Status)) return BadRequest("Status is required");

        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var order = await db.Orders
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user!.Id);

        if (order == null) return NotFound();

        order.TotalAmount = req.TotalAmount;
        order.Status = req.Status.Trim();

        switch (order)
        {
            case DigitalOrder digital:
                if (string.IsNullOrWhiteSpace(req.DownloadUrl)) return BadRequest("DownloadUrl is required for digital orders");
                digital.DownloadUrl = req.DownloadUrl.Trim();
                break;
            case PhysicalOrder physical:
                if (string.IsNullOrWhiteSpace(req.ShippingAddress)) return BadRequest("ShippingAddress is required for physical orders");
                physical.ShippingAddress = req.ShippingAddress.Trim();
                physical.TrackingNumber = string.IsNullOrWhiteSpace(req.TrackingNumber) ? null : req.TrackingNumber.Trim();
                break;
        }

        await db.SaveChangesAsync();

        return Ok(MapOrder(order));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromServices] AppDbContext db, [FromQuery] Guid? asUserId = null)
    {
        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var order = await db.Orders
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user!.Id);

        if (order == null) return NotFound();

        db.Orders.Remove(order);
        await db.SaveChangesAsync();

        return Ok(new { id });
    }

    [Authorize]
    [HttpPost("physical")]
    public async Task<IActionResult> CreatePhysical(
        [FromBody] CreatePhysicalOrderRequest req,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null)
    {
        if (req.TotalAmount <= 0) return BadRequest("TotalAmount must be greater than zero");
        if (string.IsNullOrWhiteSpace(req.ShippingAddress)) return BadRequest("ShippingAddress is required");

        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var order = new PhysicalOrder
        {
            UserId = user!.Id,
            TotalAmount = req.TotalAmount,
            ShippingAddress = req.ShippingAddress.Trim(),
            TrackingNumber = string.IsNullOrWhiteSpace(req.TrackingNumber) ? null : req.TrackingNumber.Trim(),
            Status = "Created"
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return Ok(MapOrder(order));
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

    private async Task<(AppUser? User, IActionResult? Error)> ResolveEffectiveUser(AppDbContext db, Guid? asUserId)
    {
        var currentUser = await GetOrCreateCurrentUser(db);
        if (currentUser == null) return (null, Unauthorized("Missing sub"));

        if (!asUserId.HasValue || asUserId.Value == currentUser.Id)
        {
            return (currentUser, null);
        }

        if (!User.IsInRole("admin"))
        {
            return (null, Forbid());
        }

        var targetUser = await db.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == asUserId.Value);

        if (targetUser == null)
        {
            return (null, NotFound("Target user not found"));
        }

        return (targetUser, null);
    }

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
