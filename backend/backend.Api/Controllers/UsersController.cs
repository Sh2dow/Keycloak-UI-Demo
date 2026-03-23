using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace backend.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public UsersController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers(CancellationToken ct)
    {
        var response = await _httpClient.GetAsync("http://localhost:5005/api/users", ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var users = await response.Content.ReadFromJsonAsync<IReadOnlyList<UserDto>>(ct);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetUserById(Guid id, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync($"http://localhost:5005/api/users/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var user = await response.Content.ReadFromJsonAsync<UserDto>(ct);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("http://localhost:5005/api/users", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var user = await response.Content.ReadFromJsonAsync<UserDto>(ct);
        return user == null ? NotFound() : CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PutAsJsonAsync($"http://localhost:5005/api/users/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var user = await response.Content.ReadFromJsonAsync<UserDto>(ct);
        return Ok(user);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var response = await _httpClient.DeleteAsync($"http://localhost:5005/api/users/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        return NoContent();
    }
}

public sealed record UserDto(
    Guid Id,
    string Subject,
    string Username,
    string? Email,
    DateTime CreatedAtUtc
);

public sealed record CreateUserRequest(
    string Subject,
    string Username,
    string? Email
);

public sealed record UpdateUserRequest(
    string Username,
    string? Email
);
