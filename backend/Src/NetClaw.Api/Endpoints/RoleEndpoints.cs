using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Contracts;
using NetClaw.Domains.Entities.Identity;

namespace NetClaw.Api.Endpoints;

public class RoleEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/roles", async (
            [AsParameters] GetListRequest request,
            HttpContext context,
            RoleManager<AppRole> roleManager,
            IMapper mapper) =>
        {
            var pageIndex = Math.Max(request.PageIndex ?? 0, 0);
            var pageSize = Math.Clamp(request.PageSize ?? 10, 1, 100);
            var ascending = request.Ascending ?? true;
            var searchText = request.SearchText?.Trim();

            var query = roleManager.Roles.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(role =>
                    (role.Name != null && EF.Functions.ILike(role.Name, $"%{searchText}%")) ||
                    EF.Functions.ILike(role.Description, $"%{searchText}%"));
            }

            query = (request.OrderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "updatedat" => ascending
                    ? query.OrderBy(role => role.UpdatedAt).ThenBy(role => role.Name)
                    : query.OrderByDescending(role => role.UpdatedAt).ThenByDescending(role => role.Name),
                _ => ascending
                    ? query.OrderBy(role => role.Name)
                    : query.OrderByDescending(role => role.Name),
            };

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ApiResults.Ok(
                context,
                new PagedResponse<RoleResponse>(
                    items.Adapt<List<RoleResponse>>(mapper.Config),
                    pageIndex,
                    pageSize,
                    totalItems,
                    totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize)));
        }).RequireAuthorization();

        group.MapPost("/roles", async (
            CreateRoleRequest request,
            HttpContext context,
            RoleManager<AppRole> roleManager,
            IMapper mapper) =>
        {
            var role = new AppRole
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                return ApiResults.Error(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Create role failed.",
                    details: result.Errors.ToIdentityErrorDetails());
            }

            return ApiResults.Ok(context, role.Adapt<RoleResponse>(mapper.Config));
        }).RequireAuthorization();

        group.MapPut("/roles/{roleId:guid}", async (
            Guid roleId,
            UpdateRoleRequest request,
            HttpContext context,
            RoleManager<AppRole> roleManager,
            IMapper mapper) =>
        {
            var role = await roleManager.FindByIdAsync(roleId.ToString());
            if (role is null)
            {
                return ApiResults.Error(context, StatusCodes.Status404NotFound, "Role not found.");
            }

            var nextName = request.Name.Trim();
            if (role.IsSystem && !string.Equals(role.Name, nextName, StringComparison.OrdinalIgnoreCase))
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "System roles cannot be renamed.");
            }

            role.Name = nextName;
            role.Description = request.Description.Trim();
            role.UpdatedAt = DateTimeOffset.UtcNow;

            var result = await roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                return ApiResults.Error(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Update role failed.",
                    details: result.Errors.ToIdentityErrorDetails());
            }

            return ApiResults.Ok(context, role.Adapt<RoleResponse>(mapper.Config));
        }).RequireAuthorization();

        group.MapDelete("/roles/{roleId:guid}", async (
            Guid roleId,
            HttpContext context,
            RoleManager<AppRole> roleManager) =>
        {
            var role = await roleManager.FindByIdAsync(roleId.ToString());
            if (role is null)
            {
                return Results.NoContent();
            }

            if (role.IsSystem)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "System roles cannot be deleted.");
            }

            var result = await roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "Delete role failed.");
            }

            return Results.NoContent();
        }).RequireAuthorization();
    }
}
