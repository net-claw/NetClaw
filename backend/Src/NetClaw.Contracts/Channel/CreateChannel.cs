namespace NetClaw.Contracts.Channel;

public record CreateChannel(
    string Name,
    string Kind,
    string Token,
    string? SettingsJson,
    Guid? AgentId,
    Guid? AgentTeamId,
    bool StartNow);
