using FluentResults;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Application.Features.Providers.Commands;
using NetClaw.Contracts;
using NetClaw.Contracts.Requests.Llm;
using NetClaw.Domains.Repos;
using Wolverine;

namespace NetClaw.Api.Endpoints;

public sealed class ProviderEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/providers", async (
            [AsParameters] GetProvidersRequest request,
            HttpContext context,
            IProviderRepo repo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var pageIndex = Math.Max(request.PageIndex ?? 0, 0);
            var pageSize = Math.Clamp(request.PageSize ?? 10, 1, 100);
            var ascending = request.Ascending ?? true;
            var searchText = request.SearchText?.Trim();

            var query = repo.Query().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(provider =>
                    EF.Functions.ILike(provider.Name, $"%{searchText}%") ||
                    EF.Functions.ILike(provider.ProviderType, $"%{searchText}%") ||
                    EF.Functions.ILike(provider.DefaultModel, $"%{searchText}%") ||
                    (provider.BaseUrl != null && EF.Functions.ILike(provider.BaseUrl, $"%{searchText}%")));
            }

            if (request.Active.HasValue)
            {
                query = query.Where(provider => provider.IsActive == request.Active.Value);
            }

            query = (request.OrderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "provider" => ascending
                    ? query.OrderBy(provider => provider.ProviderType).ThenBy(provider => provider.Name)
                    : query.OrderByDescending(provider => provider.ProviderType).ThenByDescending(provider => provider.Name),
                "model" => ascending
                    ? query.OrderBy(provider => provider.DefaultModel).ThenBy(provider => provider.Name)
                    : query.OrderByDescending(provider => provider.DefaultModel).ThenByDescending(provider => provider.Name),
                "updatedat" => ascending
                    ? query.OrderBy(provider => provider.UpdatedOn ?? provider.CreatedOn).ThenBy(provider => provider.Name)
                    : query.OrderByDescending(provider => provider.UpdatedOn ?? provider.CreatedOn).ThenByDescending(provider => provider.Name),
                _ => ascending
                    ? query.OrderBy(provider => provider.Name)
                    : query.OrderByDescending(provider => provider.Name),
            };

            var totalItems = await query.CountAsync(ct);
            var items = await query
                .ProjectToType<ProviderResponse>(mapper.Config)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return ApiResults.Ok(
                context,
                new PagedResponse<ProviderResponse>(
                    items,
                    pageIndex,
                    pageSize,
                    totalItems,
                    totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize)));
        }).RequireAuthorization();

        group.MapGet("/providers/{providerId:guid}", async (
            Guid providerId,
            HttpContext context,
            IProviderRepo repo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var provider = await repo.Query()
                .Where(item => item.Id == providerId)
                .ProjectToType<ProviderResponse>(mapper.Config)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);
            return provider is null
                ? ApiResults.Error(context, StatusCodes.Status404NotFound, "Provider not found.")
                : ApiResults.Ok(context, provider);
        }).RequireAuthorization();

        group.MapPost("/providers", async (
            CreateProviderRequest request,
            HttpContext context,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<ProviderData>>(
                new CreateProviderCommand(
                    request.Name,
                    request.Provider,
                    request.Model,
                    request.ApiKey,
                    request.BaseUrl,
                    request.Active));

            return ProviderEndpointMappings.ToApiResult(result, context);
        }).RequireAuthorization();

        group.MapPut("/providers/{providerId:guid}", async (
            Guid providerId,
            UpdateProviderRequest request,
            HttpContext context,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<ProviderData>>(
                new UpdateProviderCommand(
                    providerId,
                    request.Name,
                    request.Provider,
                    request.Model,
                    request.ApiKey,
                    request.BaseUrl,
                    request.Active));

            return ProviderEndpointMappings.ToApiResult(result, context);
        }).RequireAuthorization();

        group.MapDelete("/providers/{providerId:guid}", async (
            Guid providerId,
            HttpContext context,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result>(new DeleteProviderCommand(providerId));
            return ProviderEndpointMappings.ToApiResult(result, context);
        }).RequireAuthorization();
    }
}

internal static class ProviderEndpointMappings
{
    public static IResult ToApiResult(Result<ProviderData> result, HttpContext context)
    {
        if (result.IsFailed)
        {
            return ToErrorResult(result, context);
        }

        var statusCode = GetStatusCode(result.Successes.FirstOrDefault(), StatusCodes.Status200OK);
        if (statusCode == StatusCodes.Status204NoContent)
        {
            return Results.NoContent();
        }

        var data = result.ValueOrDefault;
        if (data is null)
        {
            var message = result.Successes.FirstOrDefault()?.Message ?? "Success.";
            return ApiResults.Ok(context, new MessageResponse(message));
        }

        return ApiResults.Ok(context, data.ToResponse());
    }

    public static IResult ToApiResult(Result result, HttpContext context)
    {
        if (result.IsFailed)
        {
            return ToErrorResult(result, context);
        }

        var success = result.Successes.FirstOrDefault();
        var statusCode = GetStatusCode(success, StatusCodes.Status200OK);
        if (statusCode == StatusCodes.Status204NoContent)
        {
            return Results.NoContent();
        }

        return ApiResults.Ok(context, new MessageResponse(success?.Message ?? "Success."));
    }

    public static ProviderResponse ToResponse(this ProviderData data)
        => new(
            data.Id,
            data.Name,
            data.ProviderType,
            data.DefaultModel,
            data.BaseUrl,
            data.IsActive,
            data.CreatedBy,
            data.CreatedOn,
            data.UpdatedBy,
            data.UpdatedOn);

    private static IResult ToErrorResult(IResultBase result, HttpContext context)
    {
        var error = result.Errors.FirstOrDefault();
        var statusCode = GetStatusCode(error, StatusCodes.Status400BadRequest);
        return ApiResults.Error(context, statusCode, error?.Message ?? "Request failed.");
    }

    private static int GetStatusCode(IReason? reason, int defaultStatusCode)
        => reason?.Metadata.TryGetValue("StatusCode", out var statusCode) == true && statusCode is int typedStatusCode
            ? typedStatusCode
            : defaultStatusCode;
}
