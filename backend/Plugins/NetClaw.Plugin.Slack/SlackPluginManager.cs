using System.Collections.Concurrent;
using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Logging;
using NetClaw.Plugin.Abstractions;
using SlackNet;
using SlackNet.Events;
using SlackNet.SocketMode;
using SlackNet.WebApi;

namespace NetClaw.Plugin.Slack;

public sealed class SlackPluginManager(
    IChannelInboundDispatcher dispatcher,
    Microsoft.Extensions.Logging.ILogger<SlackPluginManager> logger)
{
    private readonly ConcurrentDictionary<Guid, SlackBotRuntime> _bots = new();

    public bool IsRunning => !_bots.IsEmpty;

    public IReadOnlyCollection<SlackBotStatus> GetStatuses()
        => _bots
            .OrderBy(entry => entry.Key)
            .Select(entry => entry.Value.GetStatus())
            .ToArray();

    public async Task<Result> StartAsync(Guid channelId, string token, CancellationToken ct = default)
    {
        SlackCredentials? creds;
        try
        {
            creds = JsonSerializer.Deserialize<SlackCredentials>(token,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            creds = null;
        }

        if (creds is null
            || string.IsNullOrWhiteSpace(creds.BotToken)
            || string.IsNullOrWhiteSpace(creds.AppToken))
        {
            return Result.Fail(
                new Error("""Token must be a JSON object with "BotToken" (xoxb-...) and "AppToken" (xapp-...) fields.""")
                    .WithMetadata("StatusCode", 400));
        }

        if (_bots.TryRemove(channelId, out var existing))
            await existing.StopAsync();

        SlackBotRuntime runtime;
        try
        {
            runtime = await SlackBotRuntime.StartAsync(channelId, creds.BotToken, creds.AppToken, OnMessageAsync, logger, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Slack] Runtime start failed channelId={ChannelId}", channelId);
            return Result.Fail(new Error("Slack token is invalid or Slack is unavailable.").WithMetadata("StatusCode", 400));
        }

        _bots[channelId] = runtime;
        return Result.Ok();
    }

    public async Task<Result> StopAsync(Guid channelId, CancellationToken ct = default)
    {
        if (_bots.TryRemove(channelId, out var runtime))
        {
            await runtime.StopAsync();
            logger.LogInformation("[Slack] Runtime stopped channelId={ChannelId}", channelId);
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

    private async Task OnMessageAsync(Guid channelId, string slackChannelId, string text, string? userId)
    {
        try
        {
            var reply = await dispatcher.DispatchAsync(channelId, slackChannelId, text, userId);

            if (string.IsNullOrWhiteSpace(reply))
            {
                logger.LogInformation(
                    "[Slack] Empty reply channelId={ChannelId} slackChannelId={SlackChannelId}",
                    channelId, slackChannelId);
                return;
            }

            if (_bots.TryGetValue(channelId, out var runtime))
                await runtime.SendMessageAsync(slackChannelId, reply);

            logger.LogInformation(
                "[Slack] Reply sent channelId={ChannelId} slackChannelId={SlackChannelId}",
                channelId, slackChannelId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[Slack] Error handling message channelId={ChannelId} slackChannelId={SlackChannelId}",
                channelId, slackChannelId);
        }
    }

    private sealed class SlackBotRuntime
    {
        private readonly Guid _channelId;
        private readonly ISlackSocketModeClient _socketClient;
        private readonly ISlackApiClient _api;
        private readonly string? _botUserId;
        private readonly string? _botUsername;

        private SlackBotRuntime(
            Guid channelId,
            ISlackSocketModeClient socketClient,
            ISlackApiClient api,
            string? botUserId,
            string? botUsername)
        {
            _channelId = channelId;
            _socketClient = socketClient;
            _api = api;
            _botUserId = botUserId;
            _botUsername = botUsername;
        }

        public SlackBotStatus GetStatus() => new(_channelId, true, _botUsername, _botUserId);

        public Task SendMessageAsync(string channel, string text)
            => _api.Chat.PostMessage(new Message { Channel = channel, Text = text });

        public Task StopAsync()
        {
            _socketClient.Disconnect();
            return Task.CompletedTask;
        }

        public static async Task<SlackBotRuntime> StartAsync(
            Guid channelId,
            string botToken,
            string appToken,
            Func<Guid, string, string, string?, Task> onMessage,
            Microsoft.Extensions.Logging.ILogger logger,
            CancellationToken ct)
        {
            var messageHandler = new InlineMessageHandler(channelId, onMessage);

            var builder = new SlackServiceBuilder()
                .UseApiToken(botToken)
                .UseAppLevelToken(appToken)
                .RegisterEventHandler(messageHandler);

            var api = builder.GetApiClient();
            var socketClient = builder.GetSocketModeClient();

            await socketClient.Connect(new SocketModeConnectionOptions(), ct);

            var authTest = await api.Auth.Test(ct);

            logger.LogInformation(
                "[Slack] Started as @{Username} (id={Id}) channelId={ChannelId}",
                authTest.User, authTest.UserId, channelId);

            return new SlackBotRuntime(channelId, socketClient, api, authTest.UserId, authTest.User);
        }
    }

    private sealed class InlineMessageHandler(
        Guid channelId,
        Func<Guid, string, string, string?, Task> onMessage) : IEventHandler<MessageEvent>
    {
        public Task Handle(MessageEvent slackEvent)
        {
            // Ignore bot messages, edits, and messages without text
            if (slackEvent.BotId is not null
                || slackEvent.Subtype is not null
                || string.IsNullOrWhiteSpace(slackEvent.Text))
                return Task.CompletedTask;

            return onMessage(channelId, slackEvent.Channel, slackEvent.Text, slackEvent.User);
        }
    }
}

public sealed record SlackBotStatus(Guid ChannelId, bool Running, string? BotUsername, string? BotUserId);

public sealed record SlackCredentials(string BotToken, string AppToken);
