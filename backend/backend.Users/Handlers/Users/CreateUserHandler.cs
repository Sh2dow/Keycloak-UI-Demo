using backend.Application.Results;
using backend.Application.Users;
using backend.Dtos;
using backend.Mappers;
using backend.Requests.Users;
using MediatR;

namespace backend.Handlers.Users;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<UserWithOrdersDto>>
{
    private readonly IUserDirectory _userDirectory;

    public CreateUserHandler(IUserDirectory userDirectory)
    {
        _userDirectory = userDirectory;
    }

    public async Task<Result<UserWithOrdersDto>> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var subject = req.Subject.Trim();
        var existing = await _userDirectory.FindBySubjectAsync(subject, ct);
        if (existing != null) return Result<UserWithOrdersDto>.Conflict("User with this subject already exists.");

        var user = req.ToEntity();
        user.Subject = subject;
        user.Username = req.Username.Trim();
        user.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();

        user = await _userDirectory.CreateAsync(user, ct);

        return Result<UserWithOrdersDto>.Success(user.ToDto([]));
    }
}
