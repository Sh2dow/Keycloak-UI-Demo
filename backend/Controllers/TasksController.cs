using System.Security.Claims;
using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpGet("debugroles")]
    public async Task<IActionResult> DebugRoles(CancellationToken ct = default)
    {
        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        var result = await _mediator.Send(new DebugRolesQuery(roles), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db, [FromQuery] Guid? asUserId = null, CancellationToken ct = default)
    {
        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var tasks = await _mediator.Send(new GetTasksQuery(user!.Id), ct);
        return Ok(tasks);
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

        var task = await _mediator.Send(new GetTaskByIdQuery(id, user!.Id), ct);
        if (task == null) return NotFound();
        return Ok(task);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTaskRequest req,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest("Title is required");

        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var task = await _mediator.Send(
            new CreateTaskCommand(user!.Id, req.Title, req.Description, req.Status, req.Priority),
            ct
        );
        return Ok(task);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTaskRequest req,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null,
        CancellationToken ct = default)
    {
        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var task = await _mediator.Send(
            new UpdateTaskCommand(id, user!.Id, req.Title, req.Description, req.Status, req.Priority),
            ct
        );
        if (task == null) return NotFound();
        return Ok(task);
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

        var deleted = await _mediator.Send(new DeleteTaskCommand(id, user!.Id), ct);
        if (!deleted) return NotFound();

        return Ok(new { id });
    }

    [Authorize]
    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] AddTaskCommentRequest req,
        [FromServices] AppDbContext db,
        [FromQuery] Guid? asUserId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Content)) return BadRequest("Content is required");

        var (user, error) = await ResolveEffectiveUser(db, asUserId);
        if (error != null) return error;

        var comment = await _mediator.Send(new AddTaskCommentCommand(id, user!.Id, req.Content), ct);
        if (comment == null) return NotFound();
        return Ok(comment);
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
