using backend.Requests.Tasks;
using FluentValidation;

namespace backend.Validation.Tasks;

public sealed class AddTaskCommentCommandValidator : AbstractValidator<AddTaskCommentCommand>
{
    public AddTaskCommentCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty();
    }
}
