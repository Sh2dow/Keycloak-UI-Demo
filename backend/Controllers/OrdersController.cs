using System.Security.Claims;
using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Orders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null,
        CancellationToken ct = default)
    {
        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var orders = await _mediator.Send(new GetOrdersQuery(user!.Id), ct);
        return Ok(orders);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(
        Guid id,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null,
        CancellationToken ct = default)
    {
        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var order = await _mediator.Send(new GetOrderByIdQuery(id, user!.Id), ct);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest req,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null,
        CancellationToken ct = default)
    {
        var orderType = req.OrderType.Trim().ToLowerInvariant();

        if (orderType == "digital")
        {
            return await CreateDigital(
                new CreateDigitalOrderRequest(req.TotalAmount, req.DownloadUrl ?? string.Empty),
                db,
                asUserId,
                ct
            );
        }

        if (orderType == "physical")
        {
            return await CreatePhysical(
                new CreatePhysicalOrderRequest(req.TotalAmount, req.ShippingAddress ?? string.Empty, req.TrackingNumber),
                db,
                asUserId,
                ct
            );
        }

        return BadRequest("OrderType must be either 'digital' or 'physical'");
    }

    [Authorize]
    [HttpPost("digital")]
    public async Task<IActionResult> CreateDigital(
        [FromBody] CreateDigitalOrderRequest req,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null,
        CancellationToken ct = default)
    {
        if (req.TotalAmount <= 0) return BadRequest("TotalAmount must be greater than zero");
        if (string.IsNullOrWhiteSpace(req.DownloadUrl)) return BadRequest("DownloadUrl is required");

        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var order = await _mediator.Send(
            new CreateDigitalOrderCommand(user!.Id, req.TotalAmount, req.DownloadUrl),
            ct
        );
        return Ok(order);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateOrderRequest req,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null,
        CancellationToken ct = default)
    {
        if (req.TotalAmount <= 0) return BadRequest("TotalAmount must be greater than zero");
        if (string.IsNullOrWhiteSpace(req.Status)) return BadRequest("Status is required");

        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var result = await _mediator.Send(
            new UpdateOrderCommand(id, user!.Id, req.TotalAmount, req.Status, req.DownloadUrl, req.ShippingAddress, req.TrackingNumber),
            ct
        );
        if (result.NotFound) return NotFound();
        if (result.ValidationError != null) return BadRequest(result.ValidationError);
        return Ok(result.Order);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null,
        CancellationToken ct = default)
    {
        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var deleted = await _mediator.Send(new DeleteOrderCommand(id, user!.Id), ct);
        if (!deleted) return NotFound();

        return Ok(new { id });
    }

    [Authorize]
    [HttpPost("physical")]
    public async Task<IActionResult> CreatePhysical(
        [FromBody] CreatePhysicalOrderRequest req,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null,
        CancellationToken ct = default)
    {
        if (req.TotalAmount <= 0) return BadRequest("TotalAmount must be greater than zero");
        if (string.IsNullOrWhiteSpace(req.ShippingAddress)) return BadRequest("ShippingAddress is required");

        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var order = await _mediator.Send(
            new CreatePhysicalOrderCommand(user!.Id, req.TotalAmount, req.ShippingAddress, req.TrackingNumber),
            ct
        );
        return Ok(order);
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

}
