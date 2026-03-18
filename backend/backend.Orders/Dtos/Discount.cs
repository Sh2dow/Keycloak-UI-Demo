namespace backend.Dtos;

public sealed record Discount(
    string Type,  // "Percentage" or "Fixed"
    decimal Value
);
