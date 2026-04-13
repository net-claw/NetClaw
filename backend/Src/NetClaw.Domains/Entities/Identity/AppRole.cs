using Microsoft.AspNetCore.Identity;

namespace NetClaw.Domains.Entities.Identity;

public class AppRole : IdentityRole<Guid>
{
    public string Description { get; set; } = string.Empty;

    public bool IsSystem { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
