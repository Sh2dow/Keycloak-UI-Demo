using backend.Requests.Orders;
using FluentValidation;

namespace backend.Validation.Orders;

public sealed class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator()
    {
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x.Status).NotEmpty();
    }
}
