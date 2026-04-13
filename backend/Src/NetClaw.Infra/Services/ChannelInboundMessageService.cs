using Microsoft.Extensions.Logging;
using NetClaw.Application.Services;
using NetClaw.Domains.Repos;

namespace NetClaw.Infra.Services;

internal sealed class ChannelInboundMessageService(
    IChannelRepo repo,
    ILogger<ChannelInboundMessageService> logger) : IChannelInboundMessageService
{
    public async Task<string> HandleInboundMessageAsync(
        Guid channelId,
        string chatId,
        string text,
        string? username,
        CancellationToken cancellationToken = default)
    {
        var channel = await repo.FindAsync(channelId, cancellationToken);
        if (channel is null || channel.DeletedAt.HasValue)
        {
            logger.LogWarning(
                "Inbound message ignored because channel was not found channelId={ChannelId} chatId={ChatId}",
                channelId,
                chatId);
            return string.Empty;
        }

        if (!string.Equals(channel.Status, "running", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "Inbound message ignored because channel is not running channelId={ChannelId} kind={Kind} status={Status} chatId={ChatId}",
                channelId,
                channel.Kind,
                channel.Status,
                chatId);
            return string.Empty;
        }

        logger.LogInformation(
            "Inbound message received channelId={ChannelId} kind={Kind} chatId={ChatId} username={Username} text={Text}",
            channelId,
            channel.Kind,
            chatId,
            username,
            text);

        return "Kenh nay chua duoc lien ket voi agent. Vui long cau hinh agent cho channel nay.";
    }
}
