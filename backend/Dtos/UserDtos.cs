namespace backend.Dtos;

public sealed record CreateUserRequest(string Subject, string Username, string? Email);

public sealed record UpdateUserRequest(string Username, string? Email);

public sealed record UserWithOrdersDto(
    Guid Id,
    string Subject,
    string Username,
    string? Email,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderViewDto> Orders
);
