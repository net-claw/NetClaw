using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Contracts;
using NetClaw.Domains.Entities.Identity;

namespace NetClaw.Api.Endpoints;

public sealed class UserEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/user/me", async (HttpContext context, UserManager<AppUser> userManager, IMapper mapper) =>
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
            {
                return ApiResults.Error(context, StatusCodes.Status401Unauthorized, "Authentication required.");
            }

            return ApiResults.Ok(context, user.Adapt<UserResponse>(mapper.Config));
        }).RequireAuthorization();

        group.MapGet("/user/{userId:guid}", async (
            Guid userId,
            HttpContext context,
            UserManager<AppUser> userManager,
            IMapper mapper) =>
        {
            var user = await userManager.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId);
            if (user is null)
            {
                return ApiResults.Error(context, StatusCodes.Status404NotFound, "User not found.");
            }

            return ApiResults.Ok(context, user.Adapt<UserResponse>(mapper.Config));
        }).RequireAuthorization();

        group.MapGet("/users", async (
            [AsParameters] GetUsersRequest request,
            HttpContext context,
            UserManager<AppUser> userManager,
            IMapper mapper) =>
        {
            var pageIndex = Math.Max(request.PageIndex ?? 0, 0);
            var pageSize = Math.Clamp(request.PageSize ?? 10, 1, 100);
            var ascending = request.Ascending ?? true;
            var searchText = request.SearchText?.Trim();

            var query = userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(user =>
                    (user.Email != null && EF.Functions.ILike(user.Email, $"%{searchText}%")) ||
                    EF.Functions.ILike(user.FirstName, $"%{searchText}%") ||
                    EF.Functions.ILike(user.LastName, $"%{searchText}%"));
            }

            if (request.Active.HasValue)
            {
                var status = request.Active.Value ? UserStatuses.Active : UserStatuses.Banned;
                query = query.Where(user => user.Status == status);
            }

            query = (request.OrderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "firstname" => ascending
                    ? query.OrderBy(user => user.FirstName).ThenBy(user => user.Email)
                    : query.OrderByDescending(user => user.FirstName).ThenByDescending(user => user.Email),
                _ => ascending
                    ? query.OrderBy(user => user.Email)
                    : query.OrderByDescending(user => user.Email),
            };

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ApiResults.Ok(
                context,
                new PagedResponse<UserResponse>(
                    items.Adapt<List<UserResponse>>(mapper.Config),
                    pageIndex,
                    pageSize,
                    totalItems,
                    totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize)));
        }).RequireAuthorization();

        group.MapPost("/users", async (
            CreateUserRequest request,
            HttpContext context,
            UserManager<AppUser> userManager,
            IMapper mapper) =>
        {
            var email = request.Email.Trim();
            var firstName = request.FirstName.Trim();
            var lastName = request.LastName.Trim();

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Nickname = $"{firstName} {lastName}".Trim(),
                PhoneNumber = request.Phone?.Trim(),
                Address = request.Address?.Trim() ?? string.Empty,
                Status = UserStatuses.Active,
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return ApiResults.Error(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Create user failed.",
                    details: result.Errors.ToIdentityErrorDetails());
            }

            return ApiResults.Ok(context, user.Adapt<UserResponse>(mapper.Config));
        }).RequireAuthorization();

        group.MapPut("/users/{userId:guid}", async (
            Guid userId,
            UpdateUserRequest request,
            HttpContext context,
            UserManager<AppUser> userManager,
            IMapper mapper) =>
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return ApiResults.Error(context, StatusCodes.Status404NotFound, "User not found.");
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Nickname = $"{user.FirstName} {user.LastName}".Trim();
            user.PhoneNumber = request.Phone?.Trim();
            user.Address = request.Address?.Trim() ?? string.Empty;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ApiResults.Error(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Update user failed.",
                    details: result.Errors.ToIdentityErrorDetails());
            }

            return ApiResults.Ok(context, user.Adapt<UserResponse>(mapper.Config));
        }).RequireAuthorization();

        group.MapDelete("/users/{userId:guid}", async (
            Guid userId,
            HttpContext context,
            UserManager<AppUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Results.NoContent();
            }

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "Delete user failed.");
            }

            return Results.NoContent();
        }).RequireAuthorization();
    }
}
