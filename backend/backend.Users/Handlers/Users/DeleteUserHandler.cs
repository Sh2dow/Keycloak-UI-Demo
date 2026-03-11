using backend.Application.Results;
using backend.Application.Users;
using backend.Data;
using backend.Requests.Users;
using MediatR;

namespace backend.Handlers.Users;

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserCommand, Result<bool>>
{
    private readonly IUserDirectory _userDirectory;

    public DeleteUserHandler(IUserDirectory userDirectory)
    {
        _userDirectory = userDirectory;
    }

    public async Task<Result<bool>> Handle(DeleteUserCommand req, CancellationToken ct)
    {
        var affected = await _userDirectory.DeleteByIdAsync(req.Id, ct);

        return affected > 0
            ? Result<bool>.Success(true)
            : Result<bool>.NotFound("User not found.");
    }
}
