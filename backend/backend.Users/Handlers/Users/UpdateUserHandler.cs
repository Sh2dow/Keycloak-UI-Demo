using backend.Domain.Data;
using backend.Shared.Application.Results;
using backend.Shared.Application.Users;
using backend.Users.Dtos;
using backend.Users.Mappers;
using backend.Users.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Users.Handlers.Users;

public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Result<UserWithOrdersDto>>
{
    private readonly IUserDirectory _userDirectory;
    private readonly OrdersDbContext _ordersDb;

    public UpdateUserHandler(IUserDirectory userDirectory, OrdersDbContext ordersDb)
    {
        _userDirectory = userDirectory;
        _ordersDb = ordersDb;
    }

    public async Task<Result<UserWithOrdersDto>> Handle(UpdateUserCommand req, CancellationToken ct)
    {
        var user = await _userDirectory.FindByIdAsync(req.Id, ct);
        if (user == null) return Result<UserWithOrdersDto>.NotFound("User not found.");

        user.Username = req.Username.Trim();
        user.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
        user = await _userDirectory.UpdateAsync(user, ct);

        var orders = await _ordersDb.Orders
            .AsNoTracking()
            .Where(x => x.UserId == req.Id)
            .ToListAsync(ct);

        return Result<UserWithOrdersDto>.Success(user.ToDto(orders));
    }
}
