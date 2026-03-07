using backend.Requests.Tasks;
using FluentValidation;

namespace backend.Validation.Tasks;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
    }
}
