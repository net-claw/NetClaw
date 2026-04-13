using FluentValidation;
using NetClaw.Contracts.Channel;

namespace NetClaw.Application.Features.Channel.Validators;

public sealed class CreateChannelValidator : AbstractValidator<CreateChannel>
{
    public CreateChannelValidator()
    {
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Kind).NotEmpty();
        RuleFor(command => command.Token).NotEmpty();
    }
}
