using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetClaw.Domains.Entities.Identity;

namespace NetClaw.Infra.Identity;

public static class IdentitySeeder
{
    private const string AdminRoleName = "admin";

    public static async Task SeedIdentityAsync(this IServiceProvider services, IConfiguration configuration)
    {
        string username = configuration.GetValue<string>("AdminEmail");
        string password = configuration.GetValue<string>("AdminPassword");
        
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var adminRole = await roleManager.FindByNameAsync(AdminRoleName);
        if (adminRole is null)
        {
            adminRole = new AppRole
            {
                Id = Guid.NewGuid(),
                Name = AdminRoleName,
                NormalizedName = AdminRoleName.ToUpperInvariant(),
                Description = "System administrator",
                IsSystem = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            var createRoleResult = await roleManager.CreateAsync(adminRole);
            if (!createRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create '{AdminRoleName}' role: {string.Join(", ", createRoleResult.Errors.Select(e => e.Description))}");
            }
        }

        if(string.IsNullOrEmpty(username)|| string.IsNullOrEmpty(password))
            return;
        
        var adminUser = await userManager.FindByEmailAsync(username);
        if(adminUser is not null) return;
        adminUser = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Email = username,
            NormalizedEmail = username.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "Demo",
            LastName = "Admin",
            Nickname = "Demo Admin",
            Status = UserStatuses.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var createUserResult = await userManager.CreateAsync(adminUser, password);
        if (!createUserResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create demo admin user: {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
        }
        if (!await userManager.IsInRoleAsync(adminUser, AdminRoleName))
        {
            var addToRoleResult = await userManager.AddToRoleAsync(adminUser, AdminRoleName);
            if (!addToRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign '{AdminRoleName}' role: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}
