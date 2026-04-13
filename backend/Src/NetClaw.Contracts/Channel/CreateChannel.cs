namespace NetClaw.Contracts.Channel;

public record CreateChannel(
    string Name,
    string Kind,
    string Token,
    string? SettingsJson,
    bool StartNow);