using Xunit;

namespace NetClaw.IntegrationTests.FeatureTests.LlmTest;

[CollectionDefinition(nameof(LlmCollectionFixtureDefinition))]
public sealed class LlmCollectionFixtureDefinition : ICollectionFixture<LlmTestFixture>
{
}
