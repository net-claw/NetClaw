using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using NetClaw.Application.Services.Auth;
using NetClaw.Domains.Entities.Identity;

namespace NetClaw.Infra.Services;

public sealed class AuthService(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager) : IAuthService
{
    public async Task<AuthOperationResult> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null || user.Status == UserStatuses.Banned)
        {
            return AuthOperationResult.Failure(
                401,
                "Invalid email or password.");
        }

        var result = await signInManager.PasswordSignInAsync(user, password, true, false);
        if (!result.Succeeded)
        {
            return AuthOperationResult.Failure(
                401,
                "Invalid email or password.");
        }

        return AuthOperationResult.Success("Login successful.");
    }

    public async Task<AuthOperationResult> RefreshAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return AuthOperationResult.Failure(
                401,
                "Session expired. Please log in again.");
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null || user.Status == UserStatuses.Banned)
        {
            return AuthOperationResult.Failure(
                401,
                "Session expired. Please log in again.");
        }

        await signInManager.RefreshSignInAsync(user);
        return AuthOperationResult.Success("Token refreshed.");
    }

    public async Task<AuthOperationResult> ChangePasswordAsync(
        ClaimsPrincipal principal,
        string currentPassword,
        string newPassword)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return AuthOperationResult.Failure(
                401,
                "Authentication required.");
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            return AuthOperationResult.Failure(
                400,
                "Change password failed.",
                result.Errors.ToAuthErrorDetails());
        }

        return AuthOperationResult.Success("Password changed.");
    }
}

internal static class IdentityErrorExtensions
{
    public static IReadOnlyDictionary<string, IReadOnlyList<AuthError>> ToAuthErrorDetails(
        this IEnumerable<IdentityError> errors)
        => errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<AuthError>)group
                    .Select(error => new AuthError(error.Code, error.Description))
                    .ToList());
}
