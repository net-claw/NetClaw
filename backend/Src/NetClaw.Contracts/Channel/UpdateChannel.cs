namespace NetClaw.Contracts.Channel;

public record UpdateChannel(
    Guid ChannelId,
    string Name,
    string Kind,
    string? Token,
    string? SettingsJson);