namespace NetClaw.Plugin.Abstractions;

public sealed class PluginManifest
{
    public string Id { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string EntryAssembly { get; set; } = default!;
    public string EntryType { get; set; } = default!;
    public string HostVersion { get; set; } = default!;
    public bool Enabled { get; set; }
    public string[] Capabilities { get; set; } = Array.Empty<string>();
}