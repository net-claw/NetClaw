using System.Security.Claims;

namespace NetClaw.Application.Services.Auth;

public interface IAuthService
{
    Task<AuthOperationResult> LoginAsync(string email, string password);

    Task<AuthOperationResult> RefreshAsync(ClaimsPrincipal principal);

    Task<AuthOperationResult> ChangePasswordAsync(
        ClaimsPrincipal principal,
        string currentPassword,
        string newPassword);
}

public sealed record AuthError(string Code, string Message);

public sealed record AuthOperationResult(
    bool Succeeded,
    int StatusCode,
    string Message,
    IReadOnlyDictionary<string, IReadOnlyList<AuthError>>? Details = null)
{
    public static AuthOperationResult Success(string message)
        => new(true, 200, message);

    public static AuthOperationResult Failure(
        int statusCode,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<AuthError>>? details = null)
        => new(false, statusCode, message, details);
}
