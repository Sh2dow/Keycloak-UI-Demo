using backend.Requests.Users;
using FluentValidation;

namespace backend.Validation.Users;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
    }
}
