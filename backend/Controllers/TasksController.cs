using System.Security.Claims;
using backend.Dtos;
using backend.Requests.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var tasks = await _mediator.Send(new GetTasksQuery(pageNumber, pageSize), ct);
        return Ok(tasks);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(Guid id, CancellationToken ct = default)
    {
        var task = await _mediator.Send(new GetTaskByIdQuery(id), ct);
        if (task == null) return NotFound();
        return Ok(task);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest req, CancellationToken ct = default)
    {
        var task = await _mediator.Send(new CreateTaskCommand(req.Title, req.Description, req.Status, req.Priority), ct);
        return Ok(task);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest req, CancellationToken ct = default)
    {
        var task = await _mediator.Send(new UpdateTaskCommand(id, req.Title, req.Description, req.Status, req.Priority), ct);
        if (task == null) return NotFound();
        return Ok(task);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await _mediator.Send(new DeleteTaskCommand(id), ct);
        if (!deleted) return NotFound();
        return Ok(new { id });
    }

    [Authorize]
    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddTaskCommentRequest req, CancellationToken ct = default)
    {
        var comment = await _mediator.Send(new AddTaskCommentCommand(id, req.Content), ct);
        if (comment == null) return NotFound();
        return Ok(comment);
    }
}
