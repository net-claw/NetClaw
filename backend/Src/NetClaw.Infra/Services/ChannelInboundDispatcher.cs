using Microsoft.Extensions.DependencyInjection;
using NetClaw.Application.Services;
using NetClaw.Plugin.Abstractions;

namespace NetClaw.Infra.Services;

internal sealed class ChannelInboundDispatcher(IServiceScopeFactory scopeFactory) : IChannelInboundDispatcher
{
    public async Task<string> DispatchAsync(
        Guid channelId,
        string chatId,
        string text,
        string? username,
        CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var svc = scope.ServiceProvider.GetRequiredService<IChannelInboundMessageService>();
        return await svc.HandleInboundMessageAsync(channelId, chatId, text, username, ct);
    }
}
