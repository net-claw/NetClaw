using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Contracts;
using NetClaw.Domains.Repos;

namespace NetClaw.Api.Endpoints;

public sealed class AgentRunEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/agent-runs", async (
            HttpContext context,
            string? conversationId,
            string? targetType,
            Guid? targetId,
            IAgentRunRepo repo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var query = repo.Query().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(conversationId))
            {
                query = query.Where(item => item.ConversationId == conversationId.Trim());
            }

            if (!string.IsNullOrWhiteSpace(targetType))
            {
                var normalizedTargetType = targetType.Trim().ToLowerInvariant();
                query = query.Where(item => item.TargetType == normalizedTargetType);
            }

            if (targetId.HasValue)
            {
                query = query.Where(item => item.TargetId == targetId.Value);
            }

            var runs = (await query
                    .OrderBy(item => item.CreatedOn)
                    .ToListAsync(ct))
                .Adapt<List<AgentRunListItemResponse>>(mapper.Config);

            return ApiResults.Ok(context, runs);
        }).RequireAuthorization();

        group.MapGet("/agent-runs/{runId:guid}", async (
            Guid runId,
            HttpContext context,
            IAgentRunRepo repo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var run = await repo.Query()
                .AsNoTracking()
                .Include(item => item.Steps)
                .FirstOrDefaultAsync(item => item.Id == runId, ct);

            return run is null
                ? ApiResults.Error(context, StatusCodes.Status404NotFound, "Agent run not found.")
                : ApiResults.Ok(context, run.Adapt<AgentRunResponse>(mapper.Config));
        }).RequireAuthorization();
    }
}
