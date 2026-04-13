using System.Collections.Concurrent;
using FluentResults;
using Microsoft.Extensions.Logging;
using NetClaw.Plugin.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NetClaw.Plugin.Telegram;

public sealed class TelegramPluginManager(
    IChannelInboundDispatcher dispatcher,
    ILogger<TelegramPluginManager> logger)
{
    private readonly ConcurrentDictionary<Guid, TelegramBotRuntime> _bots = new();

    public bool IsRunning => !_bots.IsEmpty;

    public IReadOnlyCollection<TelegramBotStatus> GetStatuses()
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

        TelegramBotRuntime runtime;
        try
        {
            runtime = await TelegramBotRuntime.StartAsync(channelId, token, OnUpdateAsync, logger, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Telegram] Runtime start failed channelId={ChannelId}", channelId);
            return Result.Fail(new Error("Telegram token is invalid or Telegram is unavailable.").WithMetadata("StatusCode", 400));
        }

        _bots[channelId] = runtime;
        return Result.Ok();
    }

    public async Task<Result> StopAsync(Guid channelId, CancellationToken ct = default)
    {
        if (_bots.TryRemove(channelId, out var runtime))
        {
            await runtime.StopAsync();
            logger.LogInformation("[Telegram] Runtime stopped channelId={ChannelId}", channelId);
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

    private async Task OnUpdateAsync(
        Guid channelId,
        ITelegramBotClient bot,
        Update update,
        CancellationToken ct)
    {
        if (update.Message is not { Text: { } text } message)
            return;

        var reply = await dispatcher.DispatchAsync(
            channelId, message.Chat.Id.ToString(), text, message.From?.Username, ct);

        if (string.IsNullOrWhiteSpace(reply))
        {
            logger.LogInformation(
                "[Telegram] Empty reply channelId={ChannelId} chatId={ChatId}",
                channelId,
                message.Chat.Id);
            return;
        }

        await bot.SendMessage(message.Chat.Id, reply, cancellationToken: ct);

        logger.LogInformation(
            "[Telegram] Reply sent channelId={ChannelId} chatId={ChatId}",
            channelId,
            message.Chat.Id);
    }

    private sealed class TelegramBotRuntime(
        Guid channelId,
        CancellationTokenSource cts,
        string? username,
        long botId)
    {
        public TelegramBotStatus GetStatus() => new(channelId, true, username, botId);

        public Task StopAsync()
        {
            cts.Cancel();
            cts.Dispose();
            return Task.CompletedTask;
        }

        public static async Task<TelegramBotRuntime> StartAsync(
            Guid channelId,
            string token,
            Func<Guid, ITelegramBotClient, Update, CancellationToken, Task> updateHandler,
            ILogger logger,
            CancellationToken ct)
        {
            var bot = new TelegramBotClient(token);
            var me = await bot.GetMe(ct);
            await bot.DeleteWebhook(cancellationToken: ct);

            var cts = new CancellationTokenSource();
            bot.StartReceiving(
                updateHandler: (client, update, token) => updateHandler(channelId, client, update, token),
                errorHandler: (_, ex, _, _) =>
                {
                    logger.LogError(ex, "[Telegram] Polling error channelId={ChannelId}", channelId);
                    return Task.CompletedTask;
                },
                receiverOptions: new ReceiverOptions { AllowedUpdates = [UpdateType.Message] },
                cancellationToken: cts.Token);

            logger.LogInformation(
                "[Telegram] Started as @{Username} (id={Id}) channelId={ChannelId}",
                me.Username,
                me.Id,
                channelId);

            return new TelegramBotRuntime(channelId, cts, me.Username, me.Id);
        }
    }
}

public sealed record TelegramBotStatus(Guid ChannelId, bool Running, string? BotUsername, long BotId);
