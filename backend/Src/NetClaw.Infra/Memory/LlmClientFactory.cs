using System.ClientModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NetClaw.Application.Services;
using NetClaw.Infra.Contexts;
using OpenAI;

namespace NetClaw.Infra.Memory;

public sealed class LlmClientFactory(IServiceScopeFactory scopeFactory) : ILlmClientFactory
{
    public IChatClient Create(string modelId)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var provider = db.Providers
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No active LLM provider configured.");

        var baseUrl = provider.BaseUrl ?? GetDefaultBaseUrl(provider.ProviderType);

        var client = new OpenAIClient(
            new ApiKeyCredential(provider.EncryptedApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(baseUrl) });

        return client.GetChatClient(provider.DefaultModel).AsIChatClient();
    }

    private static string GetDefaultBaseUrl(string providerType) =>
        providerType.Trim().ToLowerInvariant() switch
        {
            "openai" => "https://api.openai.com/v1",
            "deepseek" => "https://api.deepseek.com/v1",
            "gemini" => "https://generativelanguage.googleapis.com/v1beta/openai/",
            _ => throw new InvalidOperationException($"Provider type '{providerType}' requires a BaseUrl to be configured.")
        };
}
