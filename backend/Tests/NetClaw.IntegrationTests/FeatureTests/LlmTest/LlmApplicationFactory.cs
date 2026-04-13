using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetClaw.Application.Models.Llm;
using NetClaw.Application.Services;

namespace NetClaw.IntegrationTests.FeatureTests.LlmTest;

public sealed class LlmApplicationFactory : BaseApplicationFactory<Program>
{
    public FakeLlmClientFactory FakeLlmClientFactory { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ILlmClientFactory>();
            services.RemoveAll<ContextSettings>();

            services.AddSingleton(new ContextSettings
            {
                MaxContextTokens = 40,
                RecentMessageWindow = 2,
                ResponseReserve = 0,
                SummaryModelId = "fake-summary-model",
            });
            services.AddSingleton<ILlmClientFactory>(FakeLlmClientFactory);
        });
    }
}

public sealed class FakeLlmClientFactory : ILlmClientFactory
{
    private int _createCallCount;

    public string ResponseText { get; set; } = "summary-output";

    public int CreateCallCount => _createCallCount;

    public void Reset(string responseText = "summary-output")
    {
        ResponseText = responseText;
        _createCallCount = 0;
    }

    public IChatClient Create(string modelId)
    {
        Interlocked.Increment(ref _createCallCount);
        return new FakeChatClient(ResponseText);
    }

    private sealed class FakeChatClient(string responseText) : IChatClient
    {
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText)));

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            yield return await Task.FromResult(
                new ChatResponseUpdate(ChatRole.Assistant, responseText));
        }

        public void Dispose()
        {
        }
    }
}
