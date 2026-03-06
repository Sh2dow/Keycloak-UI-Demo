using backend.Application.Results;
using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Requests.Orders;
using MediatR;

namespace backend.Handlers.Orders;

public sealed class CreatePhysicalOrderHandler : IRequestHandler<CreatePhysicalOrderCommand, Result<OrderViewDto>>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public CreatePhysicalOrderHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<Result<OrderViewDto>> Handle(CreatePhysicalOrderCommand req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var order = req.ToEntity();
        order.UserId = userId;
        order.ShippingAddress = req.ShippingAddress.Trim();
        order.TrackingNumber = string.IsNullOrWhiteSpace(req.TrackingNumber) ? null : req.TrackingNumber.Trim();
        order.Status = "Created";

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        return Result<OrderViewDto>.Success(OrderMapper.ToDto((backend.Models.Order)order));
    }
}
