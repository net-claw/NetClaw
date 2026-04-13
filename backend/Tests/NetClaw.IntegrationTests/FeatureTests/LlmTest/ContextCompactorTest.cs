using Microsoft.Extensions.DependencyInjection;
using NetClaw.Application.Services;
using NetClaw.Domains.Entities;
using NetClaw.Infra.Contexts;
using Xunit;

namespace NetClaw.IntegrationTests.FeatureTests.LlmTest;

[Collection(nameof(LlmCollectionFixtureDefinition))]
public sealed class ContextCompactorTest(LlmTestFixture fixture)
{
    [Fact]
    public async Task MaybeUpdateSummaryAsync_WhenConversationExceedsThreshold_CreatesSummaryForOlderMessages()
    {
        fixture.Factory.FakeLlmClientFactory.Reset("condensed-summary");

        var externalId = $"compact-{Guid.NewGuid():N}";
        var conversationId = await SeedConversationAsync(
            externalId,
            [
                CreateLongMessage("user", 1, "alpha"),
                CreateLongMessage("assistant", 2, "beta"),
                CreateLongMessage("user", 3, "gamma"),
                CreateLongMessage("assistant", 4, "delta"),
                CreateLongMessage("user", 5, "epsilon"),
                CreateLongMessage("assistant", 6, "zeta"),
            ]);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var compactor = scope.ServiceProvider.GetRequiredService<IContextCompactor>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await compactor.MaybeUpdateSummaryAsync(externalId);

        var summary = db.ConversationSummaries.Single(item => item.ConversationId == conversationId);

        Assert.Equal("condensed-summary", summary.SummaryText);
        Assert.Equal(4, summary.CoveredUpToSequence);
        Assert.True(summary.TokenCount > 0);
        Assert.Equal(1, fixture.Factory.FakeLlmClientFactory.CreateCallCount);
    }

    [Fact]
    public async Task MaybeUpdateSummaryAsync_WhenConversationStaysWithinThreshold_DoesNotCreateSummary()
    {
        fixture.Factory.FakeLlmClientFactory.Reset("should-not-be-used");

        var externalId = $"compact-{Guid.NewGuid():N}";
        await SeedConversationAsync(
            externalId,
            [
                ("user", 1, "short question"),
                ("assistant", 2, "short answer"),
                ("user", 3, "tiny follow up"),
            ]);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var compactor = scope.ServiceProvider.GetRequiredService<IContextCompactor>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await compactor.MaybeUpdateSummaryAsync(externalId);

        var summaries = db.ConversationSummaries
            .Where(item => item.Conversation.ExternalId == externalId)
            .ToList();

        Assert.Empty(summaries);
        Assert.Equal(0, fixture.Factory.FakeLlmClientFactory.CreateCallCount);
    }

    private async Task<Guid> SeedConversationAsync(
        string externalId,
        IReadOnlyList<(string role, int sequence, string content)> messages)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var conversation = new Conversation(
            externalId,
            "Compaction test",
            "completed",
            targetType: "agent",
            targetId: Guid.NewGuid());

        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();

        var conversationMessages = messages.Select(message =>
            new ConversationMessage(
                conversation.Id,
                message.sequence,
                message.role,
                message.content,
                externalMessageId: $"{externalId}-{message.sequence}"));

        db.ConversationMessages.AddRange(conversationMessages);
        await db.SaveChangesAsync();

        return conversation.Id;
    }

    private static (string role, int sequence, string content) CreateLongMessage(
        string role,
        int sequence,
        string token) =>
        (role, sequence, string.Join(' ', Enumerable.Repeat(token, 80)));
}
