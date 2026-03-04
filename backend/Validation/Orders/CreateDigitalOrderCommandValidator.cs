using backend.Requests.Orders;
using FluentValidation;

namespace backend.Validation.Orders;

public sealed class CreateDigitalOrderCommandValidator : AbstractValidator<CreateDigitalOrderCommand>
{
    public CreateDigitalOrderCommandValidator()
    {
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x.DownloadUrl).NotEmpty();
    }
}
