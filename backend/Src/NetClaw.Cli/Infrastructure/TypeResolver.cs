using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace NetClaw.Cli.Infrastructure;

internal sealed class TypeResolver(ServiceProvider provider) : ITypeResolver, IDisposable
{
    public object? Resolve(Type? type)
    {
        return type is null ? null : provider.GetService(type);
    }

    public void Dispose()
    {
        provider.Dispose();
    }
}
