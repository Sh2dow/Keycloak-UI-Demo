using System.Security.Claims;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    [Authorize]
    [HttpGet("debugroles")]
    public IActionResult DebugRoles() =>
        Ok(User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value));

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db)
    {
        var user = await GetOrCreateCurrentUser(db);
        if (user == null) return Unauthorized("Missing sub");

        // Demonstrates loading a user/task/comment graph without tracking while preserving
        // entity identity for repeated references (e.g. same author on multiple comments).
        var tasks = await db.Tasks
            .AsNoTrackingWithIdentityResolution()
            .Where(x => x.UserId == user.Id)
            .Include(x => x.Comments)
            .ThenInclude(x => x.Author)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        return Ok(tasks.Select(MapTask).ToList());
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(Guid id, [FromServices] AppDbContext db)
    {
        var user = await GetOrCreateCurrentUser(db);
        if (user == null) return Unauthorized("Missing sub");

        var task = await db.Tasks
            .AsNoTrackingWithIdentityResolution()
            .Include(x => x.Comments)
            .ThenInclude(x => x.Author)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);

        if (task == null) return NotFound();
        return Ok(MapTask(task));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest req, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest("Title is required");

        var user = await GetOrCreateCurrentUser(db);
        if (user == null) return Unauthorized("Missing sub");

        var task = new TaskItem
        {
            UserId = user.Id,
            Title = req.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            Status = NormalizeStatus(req.Status),
            Priority = NormalizePriority(req.Priority)
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        return Ok(MapTask(task));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest req, [FromServices] AppDbContext db)
    {
        var user = await GetOrCreateCurrentUser(db);
        if (user == null) return Unauthorized("Missing sub");

        var task = await db.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);
        if (task == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.Title))
        {
            task.Title = req.Title.Trim();
        }

        task.Description = req.Description?.Trim();
        task.Status = NormalizeStatus(req.Status);
        task.Priority = NormalizePriority(req.Priority);
        task.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(MapTask(task));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromServices] AppDbContext db)
    {
        var user = await GetOrCreateCurrentUser(db);
        if (user == null) return Unauthorized("Missing sub");

        var task = await db.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);
        if (task == null) return NotFound();

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
        return Ok(new { id });
    }

    [Authorize]
    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] AddTaskCommentRequest req,
        [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Content)) return BadRequest("Content is required");

        var user = await GetOrCreateCurrentUser(db);
        if (user == null) return Unauthorized("Missing sub");

        var task = await db.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);
        if (task == null) return NotFound();

        var comment = new TaskComment
        {
            TaskId = task.Id,
            AuthorId = user.Id,
            Content = req.Content.Trim()
        };

        db.TaskComments.Add(comment);
        await db.SaveChangesAsync();

        var created = await db.TaskComments
            .AsNoTracking()
            .Include(x => x.Author)
            .FirstAsync(x => x.Id == comment.Id);

        return Ok(new TaskCommentDto(
            created.Id,
            created.AuthorId,
            created.Author.Username,
            created.Content,
            created.CreatedAtUtc
        ));
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

    private static string NormalizeStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "todo" => "todo",
            "in-progress" => "in-progress",
            "done" => "done",
            _ => "todo"
        };
    }

    private static string NormalizePriority(string? priority)
    {
        var normalized = priority?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "low" => "low",
            "medium" => "medium",
            "high" => "high",
            _ => "medium"
        };
    }

    private static TaskItemDto MapTask(TaskItem task) =>
        new(
            task.Id,
            task.UserId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            task.Comments
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new TaskCommentDto(
                    x.Id,
                    x.AuthorId,
                    x.Author.Username,
                    x.Content,
                    x.CreatedAtUtc
                ))
                .ToList()
        );
}
