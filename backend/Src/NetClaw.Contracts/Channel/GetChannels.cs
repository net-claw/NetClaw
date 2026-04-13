namespace NetClaw.Contracts.Channel;

public sealed record GetChannels(
    int PageIndex,
    int PageSize,
    string? SearchText,
    string? OrderBy,
    bool Ascending,
    string? Kind,
    string? Status);
