namespace backend.Dtos;

public sealed record OrderItem(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);
