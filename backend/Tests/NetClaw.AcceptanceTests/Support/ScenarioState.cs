using System.Net;

namespace NetClaw.AcceptanceTests.Support;

public sealed class ScenarioState
{
    public AcceptanceApplicationFactory Factory { get; set; } = null!;
    public HttpClient Client { get; set; } = null!;
    public HttpStatusCode? LastStatusCode { get; set; }
    public string? CreatedUserEmail { get; set; }
    public string? CreatedUserPassword { get; set; }
}
