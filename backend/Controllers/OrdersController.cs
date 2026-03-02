using System.Security.Claims;
using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
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

    public sealed record CreateDigitalOrderRequest(decimal TotalAmount, string DownloadUrl);
    public sealed record CreatePhysicalOrderRequest(decimal TotalAmount, string ShippingAddress, string? TrackingNumber);
    public sealed record CreateOrderRequest(
        string OrderType,
        decimal TotalAmount,
        string? DownloadUrl,
        string? ShippingAddress,
        string? TrackingNumber
    );
    public sealed record UpdateOrderRequest(
        decimal TotalAmount,
        string Status,
        string? DownloadUrl,
        string? ShippingAddress,
        string? TrackingNumber
    );

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db)
    {
        var user = await GetOrCreateCurrentUser(db);
        if (user == null) return Unauthorized("Missing sub");

        var orders = await db.Orders
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        return Ok(orders.Select(MapOrder).ToList());
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(Guid id, [FromServices] AppDbContext db)
    {
        var user = await GetOrCreateCurrentUser(db);
        if (user == null) return Unauthorized("Missing sub");

        var order = await db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);

        if (order == null) return NotFound();

        return Ok(MapOrder(order));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest req,
        [FromServices] AppDbContext db)
    {
        var orderType = req.OrderType.Trim().ToLowerInvariant();

        if (orderType == "digital")
        {
            return await CreateDigital(new CreateDigitalOrderRequest(req.TotalAmount, req.DownloadUrl ?? string.Empty), db);
        }

        if (orderType == "physical")
        {
            return await CreatePhysical(new CreatePhysicalOrderRequest(req.TotalAmount, req.ShippingAddress ?? string.Empty, req.TrackingNumber), db);
        }

        return BadRequest("OrderType must be either 'digital' or 'physical'");
    }

    [Authorize]
    [HttpPost("digital")]
    public async Task<IActionResult> CreateDigital(
        [FromBody] CreateDigitalOrderRequest req,
        [FromServices] AppDbContext db)
    {
        if (req.TotalAmount <= 0) return BadRequest("TotalAmount must be greater than zero");
        if (string.IsNullOrWhiteSpace(req.DownloadUrl)) return BadRequest("DownloadUrl is required");

        var user = await GetOrCreateCurrentUser(db);
        if (user == null) return Unauthorized("Missing sub");

        var order = new DigitalOrder
        {
            UserId = user.Id,
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
        [FromServices] AppDbContext db)
    {
        if (req.TotalAmount <= 0) return BadRequest("TotalAmount must be greater than zero");
        if (string.IsNullOrWhiteSpace(req.Status)) return BadRequest("Status is required");

        var user = await GetOrCreateCurrentUser(db);
        if (user == null) return Unauthorized("Missing sub");

        var order = await db.Orders
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);

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
    public async Task<IActionResult> Delete(Guid id, [FromServices] AppDbContext db)
    {
        var user = await GetOrCreateCurrentUser(db);
        if (user == null) return Unauthorized("Missing sub");

        var order = await db.Orders
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);

        if (order == null) return NotFound();

        db.Orders.Remove(order);
        await db.SaveChangesAsync();

        return Ok(new { id });
    }

    [Authorize]
    [HttpPost("physical")]
    public async Task<IActionResult> CreatePhysical(
        [FromBody] CreatePhysicalOrderRequest req,
        [FromServices] AppDbContext db)
    {
        if (req.TotalAmount <= 0) return BadRequest("TotalAmount must be greater than zero");
        if (string.IsNullOrWhiteSpace(req.ShippingAddress)) return BadRequest("ShippingAddress is required");

        var user = await GetOrCreateCurrentUser(db);
        if (user == null) return Unauthorized("Missing sub");

        var order = new PhysicalOrder
        {
            UserId = user.Id,
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
