using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace backend.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TasksController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private string WithQueryString(string path) =>
        string.IsNullOrEmpty(Request.QueryString.Value)
            ? path
            : path + Request.QueryString.Value;

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, WithQueryString(path));

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
        }

        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _httpClientFactory.CreateClient("Tasks").SendAsync(request, ct);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetTasks(CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, "api/tasks", null, ct);
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
        using var response = await SendAsync(HttpMethod.Get, $"api/tasks/{id}", null, ct);
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
        using var response = await SendAsync(HttpMethod.Post, "api/tasks", request, ct);
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
        using var response = await SendAsync(HttpMethod.Put, $"api/tasks/{id}", request, ct);
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
        using var response = await SendAsync(HttpMethod.Delete, $"api/tasks/{id}", null, ct);
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
