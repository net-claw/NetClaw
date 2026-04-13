using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Contracts;
using NetClaw.Domains.Repos;

namespace NetClaw.Api.Endpoints;

public sealed class ConversationEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/conversations", async (
            HttpContext context,
            string? externalId,
            string? targetType,
            Guid? targetId,
            IConversationRepo repo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var query = repo.Query().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(externalId))
            {
                query = query.Where(item => item.ExternalId == externalId.Trim());
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

            var conversations = (await query
                    .OrderByDescending(item => item.LastMessageOn)
                    .ThenByDescending(item => item.CreatedOn)
                    .ToListAsync(ct))
                .Adapt<List<ConversationListItemResponse>>(mapper.Config);

            return ApiResults.Ok(context, conversations);
        }).RequireAuthorization();

        group.MapGet("/conversations/{conversationId:guid}", async (
            Guid conversationId,
            HttpContext context,
            IConversationRepo repo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var conversation = await repo.Query()
                .AsNoTracking()
                .Include(item => item.Messages)
                .FirstOrDefaultAsync(item => item.Id == conversationId, ct);

            return conversation is null
                ? ApiResults.Error(context, StatusCodes.Status404NotFound, "Conversation not found.")
                : ApiResults.Ok(context, conversation.Adapt<ConversationResponse>(mapper.Config));
        }).RequireAuthorization();
    }
}
