using FluentValidation;

namespace NetClaw.Application.Features.Providers.Commands;

public record UpdateProviderCommand(
    Guid ProviderId,
    string Name,
    string ProviderType,
    string DefaultModel,
    string? ApiKey,
    string? BaseUrl,
    bool IsActive);

public sealed class UpdateProviderCommandValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.ProviderType).NotEmpty();
        RuleFor(command => command.DefaultModel).NotEmpty();
    }
}