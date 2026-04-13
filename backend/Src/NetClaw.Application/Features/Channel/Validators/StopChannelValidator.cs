using FluentValidation;
using NetClaw.Contracts.Channel;

namespace NetClaw.Application.Features.Channel.Validators;

public sealed class StopChannelValidator : AbstractValidator<StopChannel>
{
    public StopChannelValidator()
    {
        RuleFor(command => command.ChannelId).NotEmpty();
    }
}
