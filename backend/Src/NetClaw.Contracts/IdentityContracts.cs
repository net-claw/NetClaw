namespace NetClaw.Contracts;

public record MessageResponse(string Message);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public record GetListRequest(
    int? PageIndex,
    int? PageSize,
    string? SearchText,
    string? OrderBy,
    bool? Ascending);

public record GetUsersRequest(
    int? PageIndex,
    int? PageSize,
    string? SearchText,
    string? OrderBy,
    bool? Ascending,
    bool? Active)
    : GetListRequest(PageIndex, PageSize, SearchText, OrderBy, Ascending);

public record CreateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string? Phone,
    string? Address);

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? Phone,
    string? Address);

public record UserResponse(
    string Id,
    string Email,
    string Nickname,
    string FirstName,
    string LastName,
    string Phone,
    string Address,
    string Status,
    string CreatedAt);

public record CreateRoleRequest(
    string Name,
    string Description);

public record UpdateRoleRequest(
    string Name,
    string Description);

public record RoleResponse(
    string Id,
    string Name,
    string Description,
    bool IsSystem,
    string CreatedAt,
    string UpdatedAt);
