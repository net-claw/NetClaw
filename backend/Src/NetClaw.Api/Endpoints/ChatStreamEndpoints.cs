using System.Text.Json;
using Microsoft.Extensions.AI;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Application.Services;

namespace NetClaw.Api.Endpoints;

public sealed class ChatStreamEndpoints : IEndpoint
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public void Map(RouteGroupBuilder group)
    {
        group.MapPost("/chat", async (
            IChatClient chatClient,
            IAgentToolService toolService,
            ChatStreamRequest request,
            CancellationToken ct) =>
        {
            var options = new ChatOptions
            {
                ConversationId = request.ThreadId,
                Tools = toolService.GetTools().ToList(),
                AdditionalProperties = new AdditionalPropertiesDictionary(),
            };

            if (!string.IsNullOrWhiteSpace(request.AgentId))
                options.AdditionalProperties["selected_agent_id"] = request.AgentId;

            if (!string.IsNullOrWhiteSpace(request.TeamId))
                options.AdditionalProperties["selected_team_id"] = request.TeamId;

            var messages = new List<ChatMessage> { new(ChatRole.User, request.Message) };
            var response = await chatClient.GetResponseAsync(messages, options, ct);

            return Results.Ok(new ChatHttpResponse(
                response.ConversationId,
                response.Text,
                response.Messages.Select(ToResponseMessage).ToArray()));
        }).RequireAuthorization();

        group.MapPost("/chat/stream", async (
            HttpContext ctx,
            IChatClient chatClient,
            IAgentToolService toolService,
            ChatStreamRequest request,
            CancellationToken ct) =>
        {
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Append("X-Accel-Buffering", "no");

            var options = new ChatOptions
            {
                ConversationId = request.ThreadId,
                Tools = toolService.GetTools().ToList(),
                AdditionalProperties = new AdditionalPropertiesDictionary(),
            };

            if (!string.IsNullOrWhiteSpace(request.AgentId))
                options.AdditionalProperties["selected_agent_id"] = request.AgentId;

            if (!string.IsNullOrWhiteSpace(request.TeamId))
                options.AdditionalProperties["selected_team_id"] = request.TeamId;

            var messages = new List<ChatMessage> { new(ChatRole.User, request.Message) };

            try
            {
                await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options, ct))
                {
                    foreach (var content in update.Contents)
                    {
                        object? evt = content switch
                        {
                            TextContent text => new { type = "text", delta = text.Text },
                            FunctionCallContent call => new
                            {
                                type = "tool_start",
                                call_id = call.CallId,
                                name = call.Name,
                                args = call.Arguments,
                            },
                            FunctionResultContent result => new
                            {
                                type = "tool_result",
                                call_id = result.CallId,
                                result = result.Result,
                            },
                            _ => null,
                        };

                        if (evt is not null)
                            await WriteSseAsync(ctx, evt, ct);
                    }
                }

                await WriteSseAsync(ctx, new { type = "done", finish_reason = "stop" }, ct);
            }
            catch (OperationCanceledException)
            {
                // client disconnected or stopped
            }
            catch (Exception ex)
            {
                await WriteSseAsync(ctx, new { type = "error", message = ex.Message }, ct);
            }
        }).RequireAuthorization();
    }

    private static async Task WriteSseAsync(HttpContext ctx, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        await ctx.Response.WriteAsync($"data: {json}\n\n", ct);
        await ctx.Response.Body.FlushAsync(ct);
    }

    private static ChatHttpMessageResponse ToResponseMessage(ChatMessage message) => new(
        message.MessageId,
        message.Role.Value,
        message.Text,
        message.Contents.Select(ToResponseContent).ToArray());

    private static object ToResponseContent(AIContent content) => content switch
    {
        TextContent text => new
        {
            type = "text",
            text = text.Text,
        },
        FunctionCallContent call => new
        {
            type = "function_call",
            call_id = call.CallId,
            name = call.Name,
            arguments = call.Arguments,
        },
        FunctionResultContent result => new
        {
            type = "function_result",
            call_id = result.CallId,
            result = result.Result,
        },
        ToolCallContent toolCall => new
        {
            type = "tool_call",
            toolCall.CallId,
        },
        ToolResultContent toolResult => new
        {
            type = "tool_result",
            call_id = toolResult.CallId,
            value = toolResult.ToString(),
        },
        DataContent data => new
        {
            type = "data",
            media_type = data.MediaType,
        },
        UriContent uri => new
        {
            type = "uri",
            uri = uri.Uri?.ToString(),
        },
        _ => new
        {
            type = content.GetType().Name,
        },
    };
}

public sealed record ChatStreamRequest(
    string ThreadId,
    string Message,
    string? AgentId = null,
    string? TeamId = null
);

public sealed record ChatHttpResponse(
    string? ConversationId,
    string? Text,
    IReadOnlyList<ChatHttpMessageResponse> Messages);

public sealed record ChatHttpMessageResponse(
    string? MessageId,
    string Role,
    string? Text,
    IReadOnlyList<object> Contents);
