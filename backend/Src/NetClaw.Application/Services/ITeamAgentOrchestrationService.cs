using Microsoft.Extensions.AI;

namespace NetClaw.Application.Services;

public interface ITeamAgentOrchestrationService
{
    Task<ChatResponse> GetResponseAsync(
        Guid teamId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        Guid teamId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);
}
