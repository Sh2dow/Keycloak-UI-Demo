using backend.Domain.Data;
using backend.Shared.Application.Users;
using backend.Tasks.Dtos;
using backend.Tasks.Mappers;
using backend.Tasks.Requests.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Tasks.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ISender _sender;

    public TasksController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItemDto>> GetTaskById(Guid id, CancellationToken ct)
    {
        var task = await _sender.Send(new GetTaskByIdQuery(id), ct);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskItemDto>>> GetTasks([FromQuery] GetTasksQuery query, CancellationToken ct)
    {
        var tasks = await _sender.Send(query, ct);
        return Ok(tasks);
    }

    [HttpPost]
    public async Task<ActionResult<TaskItemDto>> CreateTask(CreateTaskCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetTaskById), new { id = result.Value.Id }, result.Value) : BadRequest(result.Errors);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TaskItemDto>> UpdateTask(Guid id, UpdateTaskCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateTaskCommand(id, command.Title, command.Description, command.Status, command.Priority), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteTaskCommand(id), ct);
        return result.Value ? NoContent() : NotFound();
    }

    [HttpPost("{taskId}/comments")]
    public async Task<ActionResult<TaskCommentDto>> AddTaskComment(Guid taskId, AddTaskCommentCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(new AddTaskCommentCommand(taskId, command.Content), ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetTaskById), new { id = taskId }, result.Value) : BadRequest(result.Errors);
    }

    [HttpGet("debugroles")]
    public ActionResult<IReadOnlyList<string>> GetDebugRoles()
    {
        var roles = User.FindAll("realm_access.roles").Select(c => c.Value).ToList();
        return Ok(roles);
    }
}
