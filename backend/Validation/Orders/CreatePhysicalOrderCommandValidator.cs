using backend.Requests.Orders;
using FluentValidation;

namespace backend.Validation.Orders;

public sealed class CreatePhysicalOrderCommandValidator : AbstractValidator<CreatePhysicalOrderCommand>
{
    public CreatePhysicalOrderCommandValidator()
    {
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x.ShippingAddress).NotEmpty();
    }
}
