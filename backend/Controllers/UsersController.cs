using backend.Application.Users;
using backend.Dtos;
using backend.Requests.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public UsersController(IMediator mediator, IEffectiveUserAccessor effectiveUser)
    {
        _mediator = mediator;
        _effectiveUser = effectiveUser;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct = default)
    {
        await _effectiveUser.GetUserIdAsync(ct);
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
        var result = await _mediator.Send(new CreateUserCommand(req.Subject, req.Username, req.Email), ct);
        if (result.IsConflict) return Conflict("User with this subject already exists");
        return Ok(result.User);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req, CancellationToken ct = default)
    {
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
    public async Task<IActionResult> Me(CancellationToken ct = default)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var user = await _mediator.Send(new GetUserByIdQuery(userId), ct);
        return Ok(user);
    }
}
