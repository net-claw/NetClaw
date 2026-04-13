using Xunit;

namespace NetClaw.IntegrationTests.FeatureTests.AuthTest;

[CollectionDefinition(nameof(AuthCollectionFixtureDefinition))]
public class AuthCollectionFixtureDefinition : ICollectionFixture<AuthTestFixture>
{
    
}
