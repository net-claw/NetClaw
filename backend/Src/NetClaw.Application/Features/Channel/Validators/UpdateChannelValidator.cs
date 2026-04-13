using FluentValidation;
using NetClaw.Contracts.Channel;

namespace NetClaw.Application.Features.Channel.Validators;

public sealed class UpdateChannelValidator : AbstractValidator<UpdateChannel>
{
    public UpdateChannelValidator()
    {
        RuleFor(command => command.ChannelId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Kind).NotEmpty();
    }
}
