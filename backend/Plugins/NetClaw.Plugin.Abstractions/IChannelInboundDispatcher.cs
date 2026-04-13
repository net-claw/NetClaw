namespace NetClaw.Plugin.Abstractions;

public interface IChannelInboundDispatcher
{
    Task<string> DispatchAsync(
        Guid channelId,
        string chatId,
        string text,
        string? username,
        CancellationToken ct = default);
}
