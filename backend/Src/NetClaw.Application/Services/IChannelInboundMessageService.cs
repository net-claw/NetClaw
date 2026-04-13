namespace NetClaw.Application.Services;

public interface IChannelInboundMessageService
{
    Task<string> HandleInboundMessageAsync(
        Guid channelId,
        string chatId,
        string text,
        string? username,
        CancellationToken cancellationToken = default);
}
