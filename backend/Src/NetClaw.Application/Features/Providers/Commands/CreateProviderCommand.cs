using FluentValidation;

namespace NetClaw.Application.Features.Providers.Commands;

public record CreateProviderCommand(
    string Name,
    string ProviderType,
    string DefaultModel,
    string ApiKey,
    string? BaseUrl,
    bool IsActive);

public sealed class CreateProviderCommandValidator : AbstractValidator<CreateProviderCommand>
{
    public CreateProviderCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.ProviderType).NotEmpty();
        RuleFor(command => command.DefaultModel).NotEmpty();
        RuleFor(command => command.ApiKey).NotEmpty();
    }
}