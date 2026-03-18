namespace backend.Services;

public interface IOrderCalculator
{
    decimal CalculateTotal(IEnumerable<backend.Dtos.OrderItem> items);

    decimal ApplyDiscount(decimal total, backend.Dtos.Discount discount);
}
