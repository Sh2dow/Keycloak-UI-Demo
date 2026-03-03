using System.Security.Claims;
using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db, CancellationToken ct = default)
    {
        await GetOrCreateCurrentUser(db);
        var result = await _mediator.Send(new GetUsersQuery(), ct);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(Guid id, CancellationToken ct = default)
    {
        var user = await _mediator.Send(new GetUserByIdQuery(id), ct);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Subject)) return BadRequest("Subject is required");
        if (string.IsNullOrWhiteSpace(req.Username)) return BadRequest("Username is required");

        var result = await _mediator.Send(new CreateUserCommand(req.Subject, req.Username, req.Email), ct);
        if (result.IsConflict) return Conflict("User with this subject already exists");
        return Ok(result.User);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest req,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Username)) return BadRequest("Username is required");

        var updated = await _mediator.Send(new UpdateUserCommand(id, req.Username, req.Email), ct);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await _mediator.Send(new DeleteUserCommand(id), ct);
        if (!deleted) return NotFound();

        return Ok(new { id });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me([FromServices] AppDbContext db, CancellationToken ct = default)
    {
        var ensured = await GetOrCreateCurrentUser(db);
        if (ensured == null) return Unauthorized("Missing sub");

        var user = await _mediator.Send(new GetUserByIdQuery(ensured.Id), ct);
        return Ok(user);
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

}
