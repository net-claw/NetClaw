# Plugin Development

This document describes the plugin shape that NetClaw supports today.

## Purpose

Plugins let NetClaw extend runtime behavior without hard-coding everything into the main API project. The current host supports:

- lifecycle hooks
- service registration
- endpoint registration

The implementation is based on:

- [Program.cs](../../backend/Src/NetClaw.Api/Program.cs)
- [PluginLoader.cs](../../backend/Src/NetClaw.Api/PluginSystem/PluginLoader.cs)
- [IPlugin.cs](../../backend/Plugins/NetClaw.Plugin.Abstractions/IPlugin.cs)
- [PluginManifest.cs](../../backend/Plugins/NetClaw.Plugin.Abstractions/PluginManifest.cs)
- [SamplePlugin.cs](../../backend/Plugins/NetClaw.Plugin.Sample/SamplePlugin.cs)

## Plugin Contract

Core lifecycle interface:

```csharp
public interface IPlugin
{
    Task StartAsync(IPluginContext context, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

Optional extension interfaces:

- `IServiceConfigurablePlugin`: register services into DI during app startup
- `IEndpointPlugin`: map HTTP endpoints into the host application

The host provides `IPluginContext` with:

- `PluginId`
- `Services`

## Manifest Format

Each plugin is discovered from a directory containing `plugin.json`.

Current manifest shape:

```json
{
  "id": "sample.plugin",
  "version": "1.0.0",
  "entryAssembly": "NetClaw.Plugin.Sample.dll",
  "entryType": "NetClaw.Plugin.Sample.SamplePlugin",
  "hostVersion": ">=1.0.0",
  "enabled": true,
  "capabilities": ["endpoint", "services"]
}
```

Fields currently used by the host:

- `entryAssembly`
- `entryType`
- `enabled`

Other fields are useful metadata and should still be filled in consistently.

## How Loading Works

At application startup the host:

1. Reads subdirectories under the runtime `plugins` folder
2. Looks for `plugin.json`
3. Skips plugins with `enabled: false`
4. Loads dependency DLLs from the same folder if they are not already loaded
5. Loads the plugin assembly and creates the `entryType`
6. Calls `ConfigureServices()` for plugins implementing `IServiceConfigurablePlugin`
7. Calls `MapEndpoints()` for plugins implementing `IEndpointPlugin`
8. Calls `StartAsync()` after the app is built

This means plugin instances must be safe to construct via reflection and should avoid doing heavy work in constructors.

## Current Conventions

- Use a dedicated class library per plugin under `backend/Plugins/`
- Reference `NetClaw.Plugin.Abstractions`
- Keep endpoint routes namespaced under `/plugins/<plugin-name>`
- Keep runtime dependencies beside the plugin DLL in the deployed plugin folder
- Treat `ConfigureServices()` as registration-only code
- Resolve runtime services in `StartAsync()` or endpoint handlers, not during registration

## Recommended Structure

```text
backend/Plugins/
└── NetClaw.Plugin.MyPlugin/
    ├── NetClaw.Plugin.MyPlugin.csproj
    ├── MyPlugin.cs
    └── plugin.json
```

Suggested implementation shape:

```csharp
public sealed class MyPlugin : IPlugin, IServiceConfigurablePlugin, IEndpointPlugin
{
    public void ConfigureServices(IServiceCollection services, IPluginContext context)
    {
        services.AddSingleton<MyPluginService>();
    }

    public Task StartAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IPluginContext context)
    {
        var group = endpoints.MapGroup("/plugins/my-plugin");
        group.MapGet("/status", () => Results.Ok(new { ok = true }));
    }
}
```

## Packaging Notes

The running host scans a `plugins` directory under the application content root. In the current repository, example runtime plugin artifacts are checked in under:

- [backend/Src/NetClaw.Api/Plugins/sample](../../backend/Src/NetClaw.Api/Plugins/sample)
- [backend/Src/NetClaw.Api/Plugins/telegram](../../backend/Src/NetClaw.Api/Plugins/telegram)

When adding a new plugin, make sure the deployed runtime plugin folder contains:

- the plugin DLL named in `entryAssembly`
- `plugin.json`
- any dependent DLLs required at runtime

If these files are missing from the runtime `plugins` folder, the host will skip loading that plugin.

## Example References

- Sample plugin: [backend/Plugins/NetClaw.Plugin.Sample/SamplePlugin.cs](../../backend/Plugins/NetClaw.Plugin.Sample/SamplePlugin.cs)
- Telegram plugin: [backend/Plugins/NetClaw.Plugin.Telegram/TelegramPlugin.cs](../../backend/Plugins/NetClaw.Plugin.Telegram/TelegramPlugin.cs)
- Sample manifest: [backend/Plugins/NetClaw.Plugin.Sample/plugin.json](../../backend/Plugins/NetClaw.Plugin.Sample/plugin.json)

## Future Updates

If NetClaw later adds stricter plugin versioning, packaging automation, signing, or a plugin SDK, update this guide to reflect the actual runtime behavior instead of documenting aspirational design.
