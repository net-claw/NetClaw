using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using FluentResults;
using Microsoft.Extensions.Logging;
using NetClaw.Plugin.Abstractions;

namespace NetClaw.Plugin.Discord;

public sealed class DiscordPluginManager(
    IChannelInboundDispatcher dispatcher,
    ILogger<DiscordPluginManager> logger)
{
    private readonly ConcurrentDictionary<Guid, DiscordBotRuntime> _bots = new();

    public bool IsRunning => !_bots.IsEmpty;

    public IReadOnlyCollection<DiscordBotStatus> GetStatuses()
        => _bots
            .OrderBy(entry => entry.Key)
            .Select(entry => entry.Value.GetStatus())
            .ToArray();

    public async Task<Result> StartAsync(Guid channelId, string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Fail(new Error("Bot token is required.").WithMetadata("StatusCode", 400));

        if (_bots.TryRemove(channelId, out var existing))
            await existing.StopAsync();

        DiscordBotRuntime runtime;
        try
        {
            runtime = await DiscordBotRuntime.StartAsync(channelId, token, OnMessageAsync, logger, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Discord] Runtime start failed channelId={ChannelId}", channelId);
            return Result.Fail(new Error("Discord token is invalid or Discord is unavailable.").WithMetadata("StatusCode", 400));
        }

        _bots[channelId] = runtime;
        return Result.Ok();
    }

    public async Task<Result> StopAsync(Guid channelId, CancellationToken ct = default)
    {
        if (_bots.TryRemove(channelId, out var runtime))
        {
            await runtime.StopAsync();
            logger.LogInformation("[Discord] Runtime stopped channelId={ChannelId}", channelId);
        }

        return Result.Ok();
    }

    public async Task<Result> RestartAsync(Guid channelId, string token, CancellationToken ct = default)
    {
        var stop = await StopAsync(channelId, ct);
        if (stop.IsFailed) return stop;
        return await StartAsync(channelId, token, ct);
    }

    public async Task StopAllAsync()
    {
        var runtimes = _bots.ToArray();
        _bots.Clear();
        foreach (var (_, runtime) in runtimes)
            await runtime.StopAsync();
    }

    private Task OnMessageAsync(Guid channelId, SocketMessage message)
    {
        if (message.Author.IsBot || string.IsNullOrWhiteSpace(message.Content))
            return Task.CompletedTask;

        // Discord requires long-running handlers to use Task.Run
        return Task.Run(async () =>
        {
            try
            {
                var reply = await dispatcher.DispatchAsync(
                    channelId,
                    message.Channel.Id.ToString(),
                    message.Content,
                    message.Author.Username);

                if (string.IsNullOrWhiteSpace(reply))
                {
                    logger.LogInformation(
                        "[Discord] Empty reply channelId={ChannelId} discordChannelId={DiscordChannelId}",
                        channelId,
                        message.Channel.Id);
                    return;
                }

                await message.Channel.SendMessageAsync(reply);

                logger.LogInformation(
                    "[Discord] Reply sent channelId={ChannelId} discordChannelId={DiscordChannelId}",
                    channelId,
                    message.Channel.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[Discord] Error handling message channelId={ChannelId} discordChannelId={DiscordChannelId}",
                    channelId,
                    message.Channel.Id);
            }
        });
    }

    private sealed class DiscordBotRuntime
    {
        private readonly Guid _channelId;
        private readonly DiscordSocketClient _client;
        private readonly Func<SocketMessage, Task> _handler;
        private readonly string? _username;
        private readonly ulong _botId;

        private DiscordBotRuntime(
            Guid channelId,
            DiscordSocketClient client,
            Func<SocketMessage, Task> handler,
            string? username,
            ulong botId)
        {
            _channelId = channelId;
            _client = client;
            _handler = handler;
            _username = username;
            _botId = botId;
        }

        public DiscordBotStatus GetStatus() => new(_channelId, true, _username, _botId);

        public async Task StopAsync()
        {
            _client.MessageReceived -= _handler;
            await _client.StopAsync();
            await _client.LogoutAsync();
            _client.Dispose();
        }

        public static async Task<DiscordBotRuntime> StartAsync(
            Guid channelId,
            string token,
            Func<Guid, SocketMessage, Task> onMessage,
            ILogger logger,
            CancellationToken ct)
        {
            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
                    | GatewayIntents.GuildMessages
                    | GatewayIntents.MessageContent
            });

            client.Log += log =>
            {
                logger.LogInformation("[Discord:{ChannelId}] {Message}", channelId, log.ToString());
                return Task.CompletedTask;
            };

            Func<SocketMessage, Task> handler = msg => onMessage(channelId, msg);
            client.MessageReceived += handler;

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            // Wait for Ready so CurrentUser is populated
            var ready = new TaskCompletionSource();
            client.Ready += () =>
            {
                ready.TrySetResult();
                return Task.CompletedTask;
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            await using (cts.Token.Register(() => ready.TrySetCanceled()))
            {
                await ready.Task;
            }

            var username = client.CurrentUser?.Username;
            var botId = client.CurrentUser?.Id ?? 0;

            logger.LogInformation(
                "[Discord] Started as @{Username} (id={Id}) channelId={ChannelId}",
                username,
                botId,
                channelId);

            return new DiscordBotRuntime(channelId, client, handler, username, botId);
        }
    }
}

public sealed record DiscordBotStatus(Guid ChannelId, bool Running, string? BotUsername, ulong BotId);
