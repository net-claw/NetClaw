using FluentValidation;

namespace NetClaw.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password);

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        {
            RuleFor(v => v.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(v => v.Password)
                .NotEmpty()
                .MinimumLength(6);
        }
    }
}
