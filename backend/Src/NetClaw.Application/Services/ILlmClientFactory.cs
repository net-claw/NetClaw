using Microsoft.Extensions.AI;

namespace NetClaw.Application.Services;

/// <summary>
/// Creates a raw IChatClient from the first active DB provider.
/// Used by background services (MemoryProvider, ContextCompactor) to avoid
/// circular dependency with ProviderRoutingChatClient.
/// </summary>
public interface ILlmClientFactory
{
    IChatClient Create(string modelId);
}
