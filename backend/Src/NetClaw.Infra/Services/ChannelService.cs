using FluentResults;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetClaw.Application.Helpers;
using NetClaw.Application.Services;
using NetClaw.Contracts;
using NetClaw.Domains.Entities;
using NetClaw.Domains.Repos;
using NetClaw.Plugin.Abstractions;

namespace NetClaw.Infra.Services;

internal sealed class ChannelService(
    IChannelRepo repo,
    ISecretCryptoService cryptoService,
    IEnumerable<IChannelKindRuntime> runtimes,
    IMapper mapper,
    ILogger<ChannelService> logger) : IChannelService
{
    public async Task<Result<ChannelResponse>> StartAsync(
        Guid channelId,
        CancellationToken cancellationToken = default)
    {
        var channel = await repo.FindAsync(channelId, cancellationToken);
        if (channel is null || channel.DeletedAt.HasValue)
        {
            return Result.Fail<ChannelResponse>(
                new Error("Channel not found.").WithMetadata("StatusCode", 404));
        }

        var token = TryDecryptToken(channel);
        if (token is null)
        {
            channel.MarkError();
            await repo.SaveChangesAsync(cancellationToken);
            return Result.Fail<ChannelResponse>(
                new Error("Channel credentials are invalid.").WithMetadata("StatusCode", 400));
        }

        var runtime = FindRuntime(channel.Kind);
        if (runtime.IsFailed)
        {
            return Result.Fail<ChannelResponse>(runtime.Errors);
        }

        var start = await runtime.Value.StartAsync(channel.Id, token, cancellationToken);
        if (start.IsFailed)
        {
            channel.MarkError();
            await repo.SaveChangesAsync(cancellationToken);
            return Result.Fail<ChannelResponse>(start.Errors);
        }

        channel.Start();
        await repo.SaveChangesAsync(cancellationToken);

        return Result.Ok(mapper.Map<ChannelResponse>(channel))
            .WithSuccess(new Success("Channel started.").WithMetadata("StatusCode", 200));
    }

    public async Task<Result<ChannelResponse>> StopAsync(
        Guid channelId,
        CancellationToken cancellationToken = default)
    {
        var channel = await repo.FindAsync(channelId, cancellationToken);
        if (channel is null || channel.DeletedAt.HasValue)
        {
            return Result.Fail<ChannelResponse>(
                new Error("Channel not found.").WithMetadata("StatusCode", 404));
        }

        var runtime = FindRuntime(channel.Kind);
        if (runtime.IsFailed)
        {
            return Result.Fail<ChannelResponse>(runtime.Errors);
        }

        var stop = await runtime.Value.StopAsync(channel.Id, cancellationToken);
        if (stop.IsFailed)
        {
            return Result.Fail<ChannelResponse>(stop.Errors);
        }

        channel.Stop();
        await repo.SaveChangesAsync(cancellationToken);

        return Result.Ok(mapper.Map<ChannelResponse>(channel))
            .WithSuccess(new Success("Channel stopped.").WithMetadata("StatusCode", 200));
    }

    public async Task<Result<ChannelResponse>> RestartAsync(
        Guid channelId,
        CancellationToken cancellationToken = default)
    {
        var channel = await repo.FindAsync(channelId, cancellationToken);
        if (channel is null || channel.DeletedAt.HasValue)
        {
            return Result.Fail<ChannelResponse>(
                new Error("Channel not found.").WithMetadata("StatusCode", 404));
        }

        var token = TryDecryptToken(channel);
        if (token is null)
        {
            channel.MarkError();
            await repo.SaveChangesAsync(cancellationToken);
            return Result.Fail<ChannelResponse>(
                new Error("Channel credentials are invalid.").WithMetadata("StatusCode", 400));
        }

        var runtime = FindRuntime(channel.Kind);
        if (runtime.IsFailed)
        {
            return Result.Fail<ChannelResponse>(runtime.Errors);
        }

        var restart = await runtime.Value.RestartAsync(channel.Id, token, cancellationToken);
        if (restart.IsFailed)
        {
            channel.MarkError();
            await repo.SaveChangesAsync(cancellationToken);
            return Result.Fail<ChannelResponse>(restart.Errors);
        }

        channel.Restart();
        await repo.SaveChangesAsync(cancellationToken);

        return Result.Ok(mapper.Map<ChannelResponse>(channel))
            .WithSuccess(new Success("Channel restarted.").WithMetadata("StatusCode", 200));
    }

    public async Task RecoverRunningChannelsAsync(CancellationToken cancellationToken = default)
    {
        var channels = await repo.Query()
            .Where(channel =>
                channel.DeletedAt == null &&
                (channel.Status == "running" || channel.Status == "starting"))
            .ToListAsync(cancellationToken);

        foreach (var channel in channels)
        {
            var result = await StartAsync(channel.Id, cancellationToken);
            if (result.IsFailed)
            {
                logger.LogWarning(
                    "RecoverRunningChannelsAsync failed channelId={ChannelId} errors={Errors}",
                    channel.Id,
                    result.Errors.Select(error => error.Message).ToArray());
            }
            else
            {
                logger.LogInformation(
                    "RecoverRunningChannelsAsync started channelId={ChannelId} kind={Kind}",
                    channel.Id,
                    channel.Kind);
            }
        }
    }

    private Result<IChannelKindRuntime> FindRuntime(string kind)
    {
        var runtime = runtimes.FirstOrDefault(r =>
            string.Equals(r.Kind, kind, StringComparison.OrdinalIgnoreCase));

        return runtime is not null
            ? Result.Ok(runtime)
            : Result.Fail<IChannelKindRuntime>(
                new Error($"Channel kind '{kind}' is not supported.").WithMetadata("StatusCode", 400));
    }

    private string? TryDecryptToken(Channel channel)
    {
        try
        {
            return ChannelCredentialParser.TryGetToken(
                cryptoService.Decrypt(channel.EncryptedCredentials));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decrypt channel credentials channelId={ChannelId}", channel.Id);
            return null;
        }
    }
}
