using System.Text.Json;

namespace NetClaw.Application.Helpers;

public static class ChannelCredentialParser
{
    public static string? TryGetToken(string credentialsJson)
    {
        if (string.IsNullOrWhiteSpace(credentialsJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(credentialsJson);
        return document.RootElement.TryGetProperty("token", out var tokenElement)
            ? tokenElement.GetString()
            : null;
    }
}
