using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace backend.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public TasksController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetTasks(CancellationToken ct)
    {
        var response = await _httpClient.GetAsync("http://localhost:5002/api/tasks", ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var tasks = await response.Content.ReadFromJsonAsync<IReadOnlyList<TaskDto>>(ct);
        return Ok(tasks);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> GetTaskById(Guid id, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync($"http://localhost:5002/api/tasks/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var task = await response.Content.ReadFromJsonAsync<TaskDto>(ct);
        return task == null ? NotFound() : Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> CreateTask(CreateTaskRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("http://localhost:5002/api/tasks", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var task = await response.Content.ReadFromJsonAsync<TaskDto>(ct);
        return task == null ? NotFound() : CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskDto>> UpdateTask(Guid id, UpdateTaskRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PutAsJsonAsync($"http://localhost:5002/api/tasks/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var task = await response.Content.ReadFromJsonAsync<TaskDto>(ct);
        return Ok(task);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken ct)
    {
        var response = await _httpClient.DeleteAsync($"http://localhost:5002/api/tasks/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        return NoContent();
    }
}

public sealed record TaskDto(
    Guid Id,
    string Title,
    string Description,
    bool IsCompleted,
    DateTime CreatedAtUtc
);

public sealed record CreateTaskRequest(
    string Title,
    string Description,
    bool IsCompleted
);

public sealed record UpdateTaskRequest(
    string Title,
    string Description,
    bool IsCompleted
);
