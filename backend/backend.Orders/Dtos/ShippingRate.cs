namespace backend.Dtos;

public sealed record ShippingRate(
    decimal BaseRate,
    string Country
);
