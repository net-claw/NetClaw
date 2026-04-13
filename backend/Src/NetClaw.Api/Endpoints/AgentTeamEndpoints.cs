using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Contracts;
using NetClaw.Domains.Entities;
using NetClaw.Domains.Repos;

namespace NetClaw.Api.Endpoints;

public sealed class AgentTeamEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/agent-teams", async (
            [AsParameters] GetAgentTeamsRequest request,
            HttpContext context,
            IAgentTeamRepo repo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var pageIndex = Math.Max(request.PageIndex ?? 0, 0);
            var pageSize = Math.Clamp(request.PageSize ?? 10, 1, 100);
            var ascending = request.Ascending ?? true;
            var searchText = request.SearchText?.Trim();
            var status = request.Status?.Trim().ToLowerInvariant();

            var query = repo.Query()
                .AsNoTracking()
                .Include(item => item.Members)
                    .ThenInclude(item => item.Agent)
                .Include(item => item.Members)
                    .ThenInclude(item => item.ReportsToMember!)
                        .ThenInclude(member => member.Agent)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(team =>
                    EF.Functions.ILike(team.Name, $"%{searchText}%") ||
                    (team.Description != null && EF.Functions.ILike(team.Description, $"%{searchText}%")));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(team => team.Status == status);
            }

            query = (request.OrderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "status" => ascending
                    ? query.OrderBy(team => team.Status).ThenBy(team => team.Name)
                    : query.OrderByDescending(team => team.Status).ThenByDescending(team => team.Name),
                "updatedat" or "updated_at" => ascending
                    ? query.OrderBy(team => team.UpdatedOn ?? team.CreatedOn).ThenBy(team => team.Name)
                    : query.OrderByDescending(team => team.UpdatedOn ?? team.CreatedOn).ThenByDescending(team => team.Name),
                _ => ascending
                    ? query.OrderBy(team => team.Name)
                    : query.OrderByDescending(team => team.Name),
            };

            var totalItems = await query.CountAsync(ct);
            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return ApiResults.Ok(
                context,
                new PagedResponse<AgentTeamResponse>(
                    items.Adapt<List<AgentTeamResponse>>(mapper.Config),
                    pageIndex,
                    pageSize,
                    totalItems,
                    totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize)));
        }).RequireAuthorization();

        group.MapGet("/agent-teams/{teamId:guid}", async (
            Guid teamId,
            HttpContext context,
            IAgentTeamRepo repo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var team = await QueryAgentTeam(repo, teamId, ct);

            return team is null
                ? ApiResults.Error(context, StatusCodes.Status404NotFound, "Agent team not found.")
                : ApiResults.Ok(context, team.Adapt<AgentTeamResponse>(mapper.Config));
        }).RequireAuthorization();

        group.MapPost("/agent-teams", async (
            CreateAgentTeamRequest request,
            HttpContext context,
            IAgentTeamRepo teamRepo,
            IAgentTeamMemberRepo memberRepo,
            IAgentRepo agentRepo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var name = request.Name.Trim();
            if (await teamRepo.ExistsAsync(item => item.Name == name, ct))
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "Agent team name already exists.");
            }

            var validationError = await ValidateMembersAsync(request.Members, agentRepo, ct);
            if (validationError is not null)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, validationError);
            }

            var team = new AgentTeam(name, request.Description, request.Status, request.MetadataJson);
            await teamRepo.AddAsync(team, ct);

            await AddMembersAsync(team.Id, request.Members, memberRepo, ct);
            await teamRepo.SaveChangesAsync(ct);

            var created = await QueryAgentTeam(teamRepo, team.Id, ct);
            return ApiResults.Ok(context, created!.Adapt<AgentTeamResponse>(mapper.Config));
        }).RequireAuthorization();

        group.MapPut("/agent-teams/{teamId:guid}", async (
            Guid teamId,
            UpdateAgentTeamRequest request,
            HttpContext context,
            IAgentTeamRepo teamRepo,
            IAgentTeamMemberRepo memberRepo,
            IAgentRepo agentRepo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var team = await teamRepo.Query()
                .Include(item => item.Members)
                .FirstOrDefaultAsync(item => item.Id == teamId, ct);
            if (team is null)
            {
                return ApiResults.Error(context, StatusCodes.Status404NotFound, "Agent team not found.");
            }

            var name = request.Name.Trim();
            var duplicate = await teamRepo.FindAsync(item => item.Id != teamId && item.Name == name, ct);
            if (duplicate is not null)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "Agent team name already exists.");
            }

            var validationError = await ValidateMembersAsync(request.Members, agentRepo, ct);
            if (validationError is not null)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, validationError);
            }

            team.Update(name, request.Description, request.Status, request.MetadataJson);

            foreach (var existingMember in team.Members.ToList())
            {
                memberRepo.Delete(existingMember);
            }

            await AddMembersAsync(team.Id, request.Members, memberRepo, ct);
            await teamRepo.SaveChangesAsync(ct);

            var updated = await QueryAgentTeam(teamRepo, teamId, ct);
            return ApiResults.Ok(context, updated!.Adapt<AgentTeamResponse>(mapper.Config));
        }).RequireAuthorization();

        group.MapDelete("/agent-teams/{teamId:guid}", async (
            Guid teamId,
            IAgentTeamRepo repo,
            CancellationToken ct) =>
        {
            var team = await repo.FindAsync(teamId, ct);
            if (team is null)
            {
                return Results.NoContent();
            }

            repo.Delete(team);
            await repo.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private static async Task AddMembersAsync<TMemberRequest>(
        Guid teamId,
        IReadOnlyList<TMemberRequest> members,
        IAgentTeamMemberRepo memberRepo,
        CancellationToken ct)
        where TMemberRequest : class
    {
        var memberIdMap = new Dictionary<string, AgentTeamMember>(StringComparer.OrdinalIgnoreCase);
        var indexIdMap = new Dictionary<int, AgentTeamMember>();

        for (var index = 0; index < members.Count; index++)
        {
            var request = members[index];
            var member = new AgentTeamMember(
                teamId,
                Guid.Parse(GetMemberAgentId(request)!),
                GetMemberRole(request),
                GetMemberOrder(request),
                GetMemberStatus(request),
                reportsToMemberId: null,
                metadataJson: GetMemberMetadataJson(request));

            await memberRepo.AddAsync(member, ct);
            indexIdMap[index] = member;

            var requestId = GetMemberId(request);
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                memberIdMap[requestId!.Trim()] = member;
            }
        }

        foreach (var pair in indexIdMap)
        {
            var request = members[pair.Key];
            var reportsToId = GetReportsToMemberId(request);
            if (string.IsNullOrWhiteSpace(reportsToId))
            {
                continue;
            }

            if (!memberIdMap.TryGetValue(reportsToId.Trim(), out var parentMember))
            {
                throw new InvalidOperationException($"Could not resolve reportsToMemberId '{reportsToId}'.");
            }

            pair.Value.Update(
                pair.Value.AgentId,
                pair.Value.Role,
                pair.Value.Order,
                pair.Value.Status,
                parentMember.Id,
                pair.Value.MetadataJson);
        }
    }

    private static async Task<AgentTeam?> QueryAgentTeam(
        IAgentTeamRepo teamRepo,
        Guid teamId,
        CancellationToken ct) =>
        await teamRepo.Query()
            .AsNoTracking()
            .Include(item => item.Members)
                .ThenInclude(item => item.Agent)
            .Include(item => item.Members)
                .ThenInclude(item => item.ReportsToMember!)
                    .ThenInclude(member => member.Agent)
            .FirstOrDefaultAsync(item => item.Id == teamId, ct);

    private static async Task<string?> ValidateMembersAsync<TMemberRequest>(
        IReadOnlyList<TMemberRequest> members,
        IAgentRepo agentRepo,
        CancellationToken ct)
        where TMemberRequest : class
    {
        if (members.Count == 0)
        {
            return "Agent team must include at least one member.";
        }

        var invalidAgentIds = members
            .Select(GetMemberAgentId)
            .Where(agentId => !Guid.TryParse(agentId, out _))
            .ToList();
        if (invalidAgentIds.Count > 0)
        {
            return "One or more member agent ids are invalid.";
        }

        var agentIds = members
            .Select(item => Guid.Parse(GetMemberAgentId(item)!))
            .ToList();
        if (agentIds.Distinct().Count() != agentIds.Count)
        {
            return "The same agent cannot appear multiple times in one team.";
        }

        var matchedAgents = await agentRepo.Query()
            .Where(agent => agentIds.Contains(agent.Id))
            .Select(agent => agent.Id)
            .ToListAsync(ct);

        if (matchedAgents.Count != agentIds.Count)
        {
            return "One or more member agents were not found.";
        }

        var memberIds = members
            .Select(GetMemberId)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!.Trim())
            .ToList();
        if (memberIds.Count != memberIds.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            return "Each member id must be unique when provided.";
        }

        var referencedParentIds = members
            .Select(GetReportsToMemberId)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!.Trim())
            .ToList();

        if (referencedParentIds.Except(memberIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            return "Each reportsToMemberId must reference another member in the same request.";
        }

        var rootCount = members.Count(item => string.IsNullOrWhiteSpace(GetReportsToMemberId(item)));
        if (rootCount == 0)
        {
            return "Agent team must include one root member without reportsToMemberId.";
        }

        return null;
    }

    private static string? GetMemberId<TMemberRequest>(TMemberRequest request)
        where TMemberRequest : class =>
        request switch
        {
            CreateAgentTeamMemberRequest create => create.Id,
            UpdateAgentTeamMemberRequest update => update.Id,
            _ => null,
        };

    private static string? GetMemberAgentId<TMemberRequest>(TMemberRequest request)
        where TMemberRequest : class =>
        request switch
        {
            CreateAgentTeamMemberRequest create => create.AgentId?.Trim(),
            UpdateAgentTeamMemberRequest update => update.AgentId?.Trim(),
            _ => null,
        };

    private static string? GetMemberRole<TMemberRequest>(TMemberRequest request)
        where TMemberRequest : class =>
        request switch
        {
            CreateAgentTeamMemberRequest create => create.Role,
            UpdateAgentTeamMemberRequest update => update.Role,
            _ => null,
        };

    private static int GetMemberOrder<TMemberRequest>(TMemberRequest request)
        where TMemberRequest : class =>
        request switch
        {
            CreateAgentTeamMemberRequest create => create.Order,
            UpdateAgentTeamMemberRequest update => update.Order,
            _ => 0,
        };

    private static string GetMemberStatus<TMemberRequest>(TMemberRequest request)
        where TMemberRequest : class =>
        request switch
        {
            CreateAgentTeamMemberRequest create => create.Status,
            UpdateAgentTeamMemberRequest update => update.Status,
            _ => "active",
        };

    private static string? GetReportsToMemberId<TMemberRequest>(TMemberRequest request)
        where TMemberRequest : class =>
        request switch
        {
            CreateAgentTeamMemberRequest create => create.ReportsToMemberId,
            UpdateAgentTeamMemberRequest update => update.ReportsToMemberId,
            _ => null,
        };

    private static string? GetMemberMetadataJson<TMemberRequest>(TMemberRequest request)
        where TMemberRequest : class =>
        request switch
        {
            CreateAgentTeamMemberRequest create => create.MetadataJson,
            UpdateAgentTeamMemberRequest update => update.MetadataJson,
            _ => null,
        };
}
