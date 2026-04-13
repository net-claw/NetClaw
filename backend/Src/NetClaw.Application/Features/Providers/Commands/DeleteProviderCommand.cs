using FluentValidation;

namespace NetClaw.Application.Features.Providers.Commands;

public record DeleteProviderCommand(Guid ProviderId);

public sealed class DeleteProviderCommandValidator : AbstractValidator<DeleteProviderCommand>
{
    public DeleteProviderCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
    }
}