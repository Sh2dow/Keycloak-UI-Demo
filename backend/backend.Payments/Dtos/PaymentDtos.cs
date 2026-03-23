namespace backend.Payments.Dtos;

public sealed record PaymentViewDto(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    string? Provider,
    string? ProviderTransactionId
);
