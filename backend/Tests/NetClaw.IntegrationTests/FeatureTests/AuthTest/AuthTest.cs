using System.Net;
using NetClaw.AspNetCore.Extensions.Extensions;
using NetClaw.Application.Features.Auth.Commands;
using Xunit;

namespace NetClaw.IntegrationTests.FeatureTests.AuthTest;

[Collection(nameof(AuthCollectionFixtureDefinition))]
public class AuthTest(AuthTestFixture fixture)
{
    private const string EndPoint = "/api/v1/auth";
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task Login_With_InvalidCredentials_ReturnsError()
    {
        #region Login
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, $"{EndPoint}/login");
        loginRequest.SetContent(new LoginCommand("test@gmai.com", "x888888xx"));
        var loginResponse = await _client.SendAsync(loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        #endregion
    }
}
