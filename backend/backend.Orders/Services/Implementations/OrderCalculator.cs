namespace backend.Services.Implementations;

public sealed class OrderCalculator : IOrderCalculator
{
    public decimal CalculateTotal(IEnumerable<backend.Dtos.OrderItem> items)
    {
        if (items == null)
            return 0m;

        return items.Sum(item => item.Quantity * item.UnitPrice);
    }

    public decimal ApplyDiscount(decimal total, backend.Dtos.Discount discount)
    {
        if (discount == null)
            return total;

        return discount.Type.ToLowerInvariant() switch
        {
            "percentage" => total * (1 - discount.Value / 100),
            "fixed" => total - discount.Value,
            _ => total
        };
    }
}
