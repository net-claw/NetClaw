using FluentValidation;
using NetClaw.Contracts.Channel;

namespace NetClaw.Application.Features.Channel.Validators;


public sealed class StartChannelValidator : AbstractValidator<StartChannel>
{
    public StartChannelValidator()
    {
        RuleFor(command => command.ChannelId).NotEmpty();
    }
}
