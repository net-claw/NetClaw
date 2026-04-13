using Microsoft.AspNetCore.Identity;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Application.Features.Auth.Commands;
using NetClaw.Application.Services.Auth;
using NetClaw.Contracts;
using NetClaw.Domains.Entities.Identity;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using Wolverine;

namespace NetClaw.Api.Endpoints;

public sealed class AuthEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapPost("/auth/login", async (
            LoginCommand command,
            HttpContext context,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<AuthOperationResult>(command);
            return AuthEndpointMappings.ToApiResult(result, context);
        });

        group.MapPost("/auth/refresh", async (
            HttpContext context,
            IAuthService authService) =>
        {
            var result = await authService.RefreshAsync(context.User);
            return AuthEndpointMappings.ToApiResult(result, context);
        });

        group.MapPost("/auth/logout", async (HttpContext context, SignInManager<AppUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return ApiResults.Ok(context, new MessageResponse("Logged out."));
        });

        group.MapPost("/auth/change-password", async (
            ChangePasswordRequest request,
            HttpContext context,
            IAuthService authService) =>
        {
            var result = await authService.ChangePasswordAsync(
                context.User,
                request.CurrentPassword,
                request.NewPassword);

            return AuthEndpointMappings.ToApiResult(result, context);
        }).RequireAuthorization();
        
        group.AddFluentValidationAutoValidation();
    }
}

internal static class AuthEndpointMappings
{
    public static IResult ToApiResult(AuthOperationResult result, HttpContext context)
    {
        if (result.Succeeded)
        {
            return ApiResults.Ok(context, new MessageResponse(result.Message));
        }

        return ApiResults.Error(
            context,
            result.StatusCode,
            result.Message,
            details: result.Details?.ToApiDetails());
    }

    public static IDictionary<string, IReadOnlyList<ApiFieldError>> ToApiDetails(
        this IReadOnlyDictionary<string, IReadOnlyList<AuthError>> details)
        => details.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<ApiFieldError>)pair.Value
                .Select(error => new ApiFieldError(error.Code, error.Message))
                .ToList());
}
