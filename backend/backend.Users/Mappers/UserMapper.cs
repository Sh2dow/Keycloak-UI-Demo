using backend.Dtos;
using backend.Models;
using backend.Requests.Users;
using Riok.Mapperly.Abstractions;

namespace backend.Mappers;

[Mapper]
public static partial class UserMapper
{
    [MapperIgnoreTarget(nameof(AppUser.Id))]
    [MapperIgnoreTarget(nameof(AppUser.CreatedAtUtc))]
    [MapperIgnoreTarget(nameof(AppUser.Orders))]
    [MapperIgnoreTarget(nameof(AppUser.Tasks))]
    [MapperIgnoreTarget(nameof(AppUser.TaskComments))]
    public static partial AppUser ToEntity(this CreateUserCommand command);

    public static UserWithOrdersDto ToDto(this AppUser user) =>
        new(
            user.Id,
            user.Subject,
            user.Username,
            user.Email,
            user.CreatedAtUtc,
            user.Orders
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(ToOrderSummaryDto)
                .ToList()
        );

    private static UserOrderSummaryDto ToOrderSummaryDto(Order order) =>
        order switch
        {
            DigitalOrder digital => new(
                digital.Id,
                "digital",
                digital.TotalAmount,
                digital.Status,
                digital.CreatedAtUtc,
                digital.DownloadUrl,
                null,
                null
            ),
            PhysicalOrder physical => new(
                physical.Id,
                "physical",
                physical.TotalAmount,
                physical.Status,
                physical.CreatedAtUtc,
                null,
                physical.ShippingAddress,
                physical.TrackingNumber
            ),
            _ => new(
                order.Id,
                "unknown",
                order.TotalAmount,
                order.Status,
                order.CreatedAtUtc,
                null,
                null,
                null
            )
        };
}
