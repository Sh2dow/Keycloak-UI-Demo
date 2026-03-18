namespace backend.Services.Implementations;

public sealed class OrderTotalsCalculator : IOrderTotalsCalculator
{
    private readonly IOrderCalculator _orderCalculator;

    public OrderTotalsCalculator(IOrderCalculator orderCalculator)
    {
        _orderCalculator = orderCalculator ?? throw new ArgumentNullException(nameof(orderCalculator));
    }

    public decimal CalculateOrderTotal(backend.Models.Order order, IEnumerable<backend.Dtos.OrderItem> items)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        if (items == null || !items.Any())
            return 0m;

        return _orderCalculator.CalculateTotal(items);
    }

    public decimal ApplyTaxes(decimal subtotal, backend.Dtos.TaxRate taxRate)
    {
        if (taxRate == null)
            return subtotal;

        var taxAmount = subtotal * taxRate.Rate / 100;
        return subtotal + taxAmount;
    }

    public decimal ApplyShipping(decimal subtotal, backend.Dtos.ShippingRate rate)
    {
        if (rate == null)
            return subtotal;

        return subtotal + rate.BaseRate;
    }
}
