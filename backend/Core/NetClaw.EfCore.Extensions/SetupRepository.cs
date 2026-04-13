using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetClaw.EfCore.Extensions.Repos;
using NetClaw.EfCore.Extensions.Repos.Abstractions;

namespace NetClaw.EfCore.Extensions;

public static class SetupRepository
{
    private static IServiceCollection AddGenericRepositoriesCore(this IServiceCollection services) =>
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

    public static IServiceCollection AddGenericRepositories<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        if (services.All(s => s.ServiceType != typeof(DbContext)))
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());

        return services.AddGenericRepositoriesCore();
    }
}
