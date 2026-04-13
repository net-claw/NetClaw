using NetClaw.Application.Features.Auth.Commands;
using NetClaw.Application.Services.Auth;

namespace NetClaw.Application.Features.Auth.Handlers;

public sealed class LoginHandler
{
    public Task<AuthOperationResult> Handle(LoginCommand command, IAuthService authService)
        => authService.LoginAsync(command.Email, command.Password);
}
