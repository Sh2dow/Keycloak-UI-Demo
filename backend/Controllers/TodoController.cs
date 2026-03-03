using System.Security.Claims;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/todos")]
public class TodoController : ControllerBase
{
    [HttpGet("public")]
    public IActionResult Public() => Ok("Public OK");

    [Authorize]
    [HttpGet("debug")]
    public IActionResult Debug() =>
        Ok(User.Claims.Select(x => new { x.Type, x.Value }));

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
        var sub = User.Identity?.Name ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub))
            return Unauthorized("Missing sub");

        var items = await db.TodoItems
            .Where(x => x.UserSub == sub)
            .ToListAsync();

        return Ok(items);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTodoRequest req, [FromServices] AppDbContext db)
    {
        Console.WriteLine("Authenticated: " + User.Identity?.IsAuthenticated);
        var sub = User.Identity?.Name ?? User.FindFirstValue("sub");
        Console.WriteLine("Sub: " + sub);
        if (string.IsNullOrWhiteSpace(sub)) return Unauthorized("Missing sub");

        var item = new TodoItem
        {
            Title = req.Title,
            UserSub = sub
        };

        db.TodoItems.Add(item);
        await db.SaveChangesAsync();

        return Ok(item);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("admin")]
    public IActionResult AdminOnly() => Ok("Admin OK");
}
