using Microsoft.AspNetCore.Identity;

namespace NetClaw.Domains.Entities.Identity;

public class AppUser : IdentityUser<Guid>
{
    public string Nickname { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Status { get; set; } = UserStatuses.Active;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
