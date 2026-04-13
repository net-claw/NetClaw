using FluentResults;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Contracts;
using NetClaw.Contracts.Channel;
using Wolverine;

namespace NetClaw.Api.Endpoints;

public sealed class ChannelEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/channels", async (
            [AsParameters] GetChannelsRequest request,
            HttpContext context,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            var pageIndex = Math.Max(request.PageIndex ?? 0, 0);
            var pageSize = Math.Clamp(request.PageSize ?? 10, 1, 100);
            var ascending = request.Ascending ?? true;

            var result = await bus.InvokeAsync<ChannelPageResponse>(
                new GetChannels(
                    pageIndex,
                    pageSize,
                    request.SearchText,
                    request.OrderBy,
                    ascending,
                    request.Kind,
                    request.Status),
                ct);

            return ApiResults.Ok(
                context,
                new PagedResponse<ChannelResponse>(
                    result.Items,
                    result.PageIndex,
                    result.PageSize,
                    result.TotalItems,
                    result.TotalPage));
        }).RequireAuthorization();

        group.MapGet("/channels/{channelId:guid}", async (
            Guid channelId,
            HttpContext context,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            var channel = await bus.InvokeAsync<ChannelResponse?>(
                new GetChannelById(channelId),
                ct);

            return channel is null
                ? ApiResults.Error(context, StatusCodes.Status404NotFound, "Channel not found.")
                : ApiResults.Ok(context, channel);
        }).RequireAuthorization();

        group.MapPost("/channels", async (
            CreateChannelRequest request,
            HttpContext context,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            var result = await bus.InvokeAsync<Result<ChannelResponse>>(
                new CreateChannel(
                    request.Name,
                    request.Kind,
                    request.Token,
                    request.SettingsJson,
                    request.StartNow),
                ct);

            return ChannelEndpointMappings.ToApiResult(result, context);
        }).RequireAuthorization();

        group.MapPut("/channels/{channelId:guid}", async (
            Guid channelId,
            UpdateChannelRequest request,
            HttpContext context,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            var result = await bus.InvokeAsync<Result<ChannelResponse>>(
                new UpdateChannel(
                    channelId,
                    request.Name,
                    request.Kind,
                    request.Token,
                    request.SettingsJson),
                ct);

            return ChannelEndpointMappings.ToApiResult(result, context);
        }).RequireAuthorization();

        group.MapDelete("/channels/{channelId:guid}", async (
            Guid channelId,
            HttpContext context,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            var result = await bus.InvokeAsync<Result>(new DeleteChannel(channelId), ct);
            return ChannelEndpointMappings.ToApiResult(result, context);
        }).RequireAuthorization();

        group.MapPost("/channels/{channelId:guid}/start", async (
            Guid channelId,
            HttpContext context,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            var result = await bus.InvokeAsync<Result<ChannelResponse>>(new StartChannel(channelId), ct);
            return ChannelEndpointMappings.ToApiResult(result, context);
        }).RequireAuthorization();

        group.MapPost("/channels/{channelId:guid}/stop", async (
            Guid channelId,
            HttpContext context,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            var result = await bus.InvokeAsync<Result<ChannelResponse>>(new StopChannel(channelId), ct);
            return ChannelEndpointMappings.ToApiResult(result, context);
        }).RequireAuthorization();

        group.MapPost("/channels/{channelId:guid}/restart", async (
            Guid channelId,
            HttpContext context,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            var result = await bus.InvokeAsync<Result<ChannelResponse>>(new RestartChannel(channelId), ct);
            return ChannelEndpointMappings.ToApiResult(result, context);
        }).RequireAuthorization();
    }
}

internal static class ChannelEndpointMappings
{
    public static IResult ToApiResult(Result<ChannelResponse> result, HttpContext context)
    {
        if (result.IsFailed)
        {
            return ToErrorResult(result, context);
        }

        var statusCode = GetStatusCode(result.Successes.FirstOrDefault(), StatusCodes.Status200OK);
        if (statusCode == StatusCodes.Status204NoContent)
        {
            return Results.NoContent();
        }

        var data = result.ValueOrDefault;
        if (data is null)
        {
            var message = result.Successes.FirstOrDefault()?.Message ?? "Success.";
            return ApiResults.Ok(context, new MessageResponse(message));
        }

        return ApiResults.Ok(context, data);
    }

    public static IResult ToApiResult(Result result, HttpContext context)
    {
        if (result.IsFailed)
        {
            return ToErrorResult(result, context);
        }

        var success = result.Successes.FirstOrDefault();
        var statusCode = GetStatusCode(success, StatusCodes.Status200OK);
        if (statusCode == StatusCodes.Status204NoContent)
        {
            return Results.NoContent();
        }

        return ApiResults.Ok(context, new MessageResponse(success?.Message ?? "Success."));
    }

    private static IResult ToErrorResult(IResultBase result, HttpContext context)
    {
        var error = result.Errors.FirstOrDefault();
        var statusCode = GetStatusCode(error, StatusCodes.Status400BadRequest);
        return ApiResults.Error(context, statusCode, error?.Message ?? "Request failed.");
    }

    private static int GetStatusCode(IReason? reason, int defaultStatusCode)
        => reason?.Metadata.TryGetValue("StatusCode", out var statusCode) == true && statusCode is int typedStatusCode
            ? typedStatusCode
            : defaultStatusCode;
}
