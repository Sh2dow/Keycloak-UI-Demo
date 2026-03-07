using backend.Dtos;
using backend.Models;
using backend.Requests.Orders;
using Riok.Mapperly.Abstractions;

namespace backend.Mappers;

[Mapper]
public static partial class OrderMapper
{
    [MapperIgnoreTarget(nameof(DigitalOrder.Id))]
    [MapperIgnoreTarget(nameof(DigitalOrder.UserId))]
    [MapperIgnoreTarget(nameof(DigitalOrder.User))]
    [MapperIgnoreTarget(nameof(DigitalOrder.Status))]
    [MapperIgnoreTarget(nameof(DigitalOrder.CreatedAtUtc))]
    public static partial DigitalOrder ToEntity(this CreateDigitalOrderCommand command);

    [MapperIgnoreTarget(nameof(PhysicalOrder.Id))]
    [MapperIgnoreTarget(nameof(PhysicalOrder.UserId))]
    [MapperIgnoreTarget(nameof(PhysicalOrder.User))]
    [MapperIgnoreTarget(nameof(PhysicalOrder.Status))]
    [MapperIgnoreTarget(nameof(PhysicalOrder.CreatedAtUtc))]
    public static partial PhysicalOrder ToEntity(this CreatePhysicalOrderCommand command);

    public static OrderViewDto ToDto(this Order order) =>
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
