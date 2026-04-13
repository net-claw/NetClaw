using FluentValidation;
using NetClaw.Contracts.Channel;

namespace NetClaw.Application.Features.Channel.Validators;

public sealed class DeleteChannelValidator : AbstractValidator<DeleteChannel>
{
    public DeleteChannelValidator()
    {
        RuleFor(command => command.ChannelId).NotEmpty();
    }
}
