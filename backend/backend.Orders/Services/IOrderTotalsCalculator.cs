namespace backend.Services;

public interface IOrderTotalsCalculator
{
    decimal CalculateOrderTotal(backend.Models.Order order, IEnumerable<backend.Dtos.OrderItem> items);

    decimal ApplyTaxes(decimal subtotal, backend.Dtos.TaxRate taxRate);

    decimal ApplyShipping(decimal subtotal, backend.Dtos.ShippingRate rate);
}
