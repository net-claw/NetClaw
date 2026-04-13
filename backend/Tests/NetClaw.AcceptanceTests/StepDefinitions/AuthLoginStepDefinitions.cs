using System.Net;
using NetClaw.AspNetCore.Extensions.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetClaw.AcceptanceTests.Support;
using NetClaw.Application.Features.Auth.Commands;
using NetClaw.Contracts;
using Reqnroll;

namespace NetClaw.AcceptanceTests.StepDefinitions;

[Binding]
public sealed class AuthLoginStepDefinitions(ScenarioState scenarioState)
{
    private const string AuthEndpoint = "/api/v1/auth";
    private const string UsersEndpoint = "/api/v1/users";

    [Given("the acceptance test client is ready")]
    public void GivenTheAcceptanceTestClientIsReady()
    {
        Assert.IsNotNull(scenarioState.Client);
    }

    [Given("I login with the seeded admin account")]
    public async Task GivenILoginWithTheSeededAdminAccount()
    {
        scenarioState.LastStatusCode = await LoginAsync("admin.system@example.com", "R@nd0mP@$$");
        Assert.AreEqual(HttpStatusCode.OK, scenarioState.LastStatusCode);
    }

    [Given("I create a new active user")]
    public async Task GivenICreateANewActiveUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"acceptance.{suffix}@example.com";
        var password = "R@nd0mP@$$!";

        var request = new HttpRequestMessage(HttpMethod.Post, UsersEndpoint);
        request.SetContent(new CreateUserRequest(
            email,
            "Acceptance",
            "User",
            password,
            null,
            null));

        var response = await scenarioState.Client.SendAsync(request);

        scenarioState.LastStatusCode = response.StatusCode;
        scenarioState.CreatedUserEmail = email;
        scenarioState.CreatedUserPassword = password;

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Given("I logout")]
    public async Task GivenILogout()
    {
        var response = await scenarioState.Client.PostAsync($"{AuthEndpoint}/logout", content: null);
        scenarioState.LastStatusCode = response.StatusCode;
    }

    [When("I login with the new user")]
    public async Task WhenILoginWithTheNewUser()
    {
        Assert.IsNotNull(scenarioState.CreatedUserEmail);
        Assert.IsNotNull(scenarioState.CreatedUserPassword);

        scenarioState.LastStatusCode = await LoginAsync(
            scenarioState.CreatedUserEmail,
            scenarioState.CreatedUserPassword);
    }

    [Then("the response status should be OK")]
    public void ThenTheResponseStatusShouldBeOk()
    {
        Assert.AreEqual(HttpStatusCode.OK, scenarioState.LastStatusCode);
    }

    private async Task<HttpStatusCode> LoginAsync(string email, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{AuthEndpoint}/login");
        request.SetContent(new LoginCommand(email, password));

        var response = await scenarioState.Client.SendAsync(request);
        return response.StatusCode;
    }
}
