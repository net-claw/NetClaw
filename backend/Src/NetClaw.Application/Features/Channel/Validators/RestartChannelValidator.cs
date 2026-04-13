using FluentValidation;
using NetClaw.Contracts.Channel;

namespace NetClaw.Application.Features.Channel.Validators;

public sealed class RestartChannelValidator : AbstractValidator<RestartChannel>
{
    public RestartChannelValidator()
    {
        RuleFor(command => command.ChannelId).NotEmpty();
    }
}
