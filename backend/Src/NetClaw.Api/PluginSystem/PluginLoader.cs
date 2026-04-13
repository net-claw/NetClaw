using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NetClaw.Plugin.Abstractions;

namespace NetClaw.Api.PluginSystem;

public sealed class PluginLoader
{
    private readonly List<(PluginManifest Manifest, IPlugin Plugin)> _loaded = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public void LoadAll(string pluginsDirectory)
    {
        if (!Directory.Exists(pluginsDirectory))
            return;

        foreach (var dir in Directory.GetDirectories(pluginsDirectory))
        {
            var manifestPath = Path.Combine(dir, "plugin.json");
            if (!File.Exists(manifestPath))
                continue;

            var manifest = JsonSerializer.Deserialize<PluginManifest>(
                File.ReadAllText(manifestPath), JsonOptions);

            if (manifest is null || !manifest.Enabled)
                continue;

            var assemblyPath = Path.Combine(dir, manifest.EntryAssembly);
            if (!File.Exists(assemblyPath))
                continue;

            // Load dependency DLLs first (e.g. Telegram.Bot.dll),
            // skip ones already loaded by the host.
            var loadedNames = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var dll in Directory.GetFiles(dir, "*.dll"))
            {
                var dllName = AssemblyName.GetAssemblyName(dll).Name;
                if (!loadedNames.Contains(dllName))
                    AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
            }

            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            var type = assembly.GetType(manifest.EntryType)
                ?? throw new InvalidOperationException(
                    $"Type '{manifest.EntryType}' not found in '{manifest.EntryAssembly}'");

            var plugin = (IPlugin)Activator.CreateInstance(type)!;
            _loaded.Add((manifest, plugin));
        }
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Services not yet built during builder phase — plugins should only register, not resolve
        var context = new PluginContext("host", NullServiceProvider.Instance);
        foreach (var (_, plugin) in _loaded)
        {
            if (plugin is IServiceConfigurablePlugin configurable)
                configurable.ConfigureServices(services, context);
        }
    }

    private sealed class NullServiceProvider : IServiceProvider
    {
        public static readonly NullServiceProvider Instance = new();
        public object? GetService(Type serviceType) =>
            throw new InvalidOperationException(
                "Services are not available during plugin ConfigureServices phase.");
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IServiceProvider appServices)
    {
        var context = new PluginContext("host", appServices);
        foreach (var (_, plugin) in _loaded)
        {
            if (plugin is IEndpointPlugin endpointPlugin)
                endpointPlugin.MapEndpoints(endpoints, context);
        }
    }

    public async Task StartAllAsync(IServiceProvider appServices, CancellationToken cancellationToken = default)
    {
        var context = new PluginContext("host", appServices);
        foreach (var (_, plugin) in _loaded)
            await plugin.StartAsync(context, cancellationToken);
    }

    public async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (_, plugin) in _loaded)
            await plugin.StopAsync(cancellationToken);
    }

    public IReadOnlyList<PluginManifest> LoadedManifests =>
        _loaded.ConvertAll(x => x.Manifest).AsReadOnly();
}
