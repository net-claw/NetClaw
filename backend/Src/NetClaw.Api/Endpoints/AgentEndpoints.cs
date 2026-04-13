using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Contracts;
using NetClaw.Domains.Entities;
using NetClaw.Domains.Repos;

namespace NetClaw.Api.Endpoints;

public sealed class AgentEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/agents", async (
            [AsParameters] GetAgentsRequest request,
            HttpContext context,
            IAgentRepo repo,
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
                .Include(agent => agent.AgentProviders)
                .ThenInclude(agentProvider => agentProvider.Provider)
                .Include(agent => agent.AgentSkills)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(agent =>
                    EF.Functions.ILike(agent.Name, $"%{searchText}%") ||
                    EF.Functions.ILike(agent.Role, $"%{searchText}%") ||
                    EF.Functions.ILike(agent.Kind, $"%{searchText}%") ||
                    EF.Functions.ILike(agent.Type, $"%{searchText}%"));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(agent => agent.Status == status);
            }

            query = (request.OrderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "role" => ascending
                    ? query.OrderBy(agent => agent.Role).ThenBy(agent => agent.Name)
                    : query.OrderByDescending(agent => agent.Role).ThenByDescending(agent => agent.Name),
                "kind" => ascending
                    ? query.OrderBy(agent => agent.Kind).ThenBy(agent => agent.Name)
                    : query.OrderByDescending(agent => agent.Kind).ThenByDescending(agent => agent.Name),
                _ => ascending
                    ? query.OrderBy(agent => agent.Name)
                    : query.OrderByDescending(agent => agent.Name),
            };

            var totalItems = await query.CountAsync(ct);
            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return ApiResults.Ok(
                context,
                new PagedResponse<AgentResponse>(
                    items.Adapt<List<AgentResponse>>(mapper.Config),
                    pageIndex,
                    pageSize,
                    totalItems,
                    totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize)));
        }).RequireAuthorization();

        group.MapGet("/agents/{agentId:guid}", async (
            Guid agentId,
            HttpContext context,
            IAgentRepo repo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var agent = await repo.Query()
                .AsNoTracking()
                .Include(item => item.AgentProviders)
                .ThenInclude(item => item.Provider)
                .Include(item => item.AgentSkills)
                .FirstOrDefaultAsync(item => item.Id == agentId, ct);

            return agent is null
                ? ApiResults.Error(context, StatusCodes.Status404NotFound, "Agent not found.")
                : ApiResults.Ok(context, agent.Adapt<AgentResponse>(mapper.Config));
        }).RequireAuthorization();

        group.MapPost("/agents", async (
            CreateAgentRequest request,
            HttpContext context,
            IAgentRepo agentRepo,
            IAgentProviderRepo agentProviderRepo,
            IAgentSkillRepo agentSkillRepo,
            IProviderRepo providerRepo,
            ISkillRepo skillRepo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var name = request.Name.Trim();
            if (await agentRepo.ExistsAsync(agent => agent.Name == name, ct))
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "Agent name already exists.");
            }

            var providerIds = ParseEntityIds(request.ProviderIds);
            var providers = await providerRepo.Query()
                .Where(provider => providerIds.Contains(provider.Id))
                .ToListAsync(ct);

            if (providers.Count != providerIds.Count)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "One or more providers were not found.");
            }

            var skillIds = ParseEntityIds(request.SkillIds);
            var skills = await skillRepo.Query()
                .Where(skill => skillIds.Contains(skill.Id))
                .ToListAsync(ct);

            if (skills.Count != skillIds.Count)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "One or more skills were not found.");
            }

            var agent = new Agent(
                name,
                request.Role,
                request.Kind,
                request.Type,
                request.Status,
                request.SystemPrompt,
                request.ModelOverride,
                request.Temperature,
                request.MaxTokens,
                request.MetadataJson);

            await agentRepo.AddAsync(agent, ct);

            for (var index = 0; index < providerIds.Count; index++)
            {
                await agentProviderRepo.AddAsync(
                    new AgentProvider(agent.Id, providerIds[index], index, "active"),
                    ct);
            }

            foreach (var skillId in skillIds)
            {
                await agentSkillRepo.AddAsync(
                    new AgentSkill(agent.Id, skillId, "active"),
                    ct);
            }

            await agentRepo.SaveChangesAsync(ct);

            var created = await agentRepo.Query()
                .AsNoTracking()
                .Include(item => item.AgentProviders)
                .ThenInclude(item => item.Provider)
                .Include(item => item.AgentSkills)
                .FirstAsync(item => item.Id == agent.Id, ct);

            return ApiResults.Ok(context, created.Adapt<AgentResponse>(mapper.Config));
        }).RequireAuthorization();

        group.MapPut("/agents/{agentId:guid}", async (
            Guid agentId,
            UpdateAgentRequest request,
            HttpContext context,
            IAgentRepo agentRepo,
            IAgentProviderRepo agentProviderRepo,
            IAgentSkillRepo agentSkillRepo,
            IProviderRepo providerRepo,
            ISkillRepo skillRepo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var agent = await agentRepo.Query()
                .Include(item => item.AgentProviders)
                .Include(item => item.AgentSkills)
                .FirstOrDefaultAsync(item => item.Id == agentId, ct);
            if (agent is null)
            {
                return ApiResults.Error(context, StatusCodes.Status404NotFound, "Agent not found.");
            }

            var name = request.Name.Trim();
            var duplicate = await agentRepo.FindAsync(item => item.Id != agentId && item.Name == name, ct);
            if (duplicate is not null)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "Agent name already exists.");
            }

            var providerIds = ParseEntityIds(request.ProviderIds);
            var providers = await providerRepo.Query()
                .Where(provider => providerIds.Contains(provider.Id))
                .ToListAsync(ct);

            if (providers.Count != providerIds.Count)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "One or more providers were not found.");
            }

            var skillIds = ParseEntityIds(request.SkillIds);
            var skills = await skillRepo.Query()
                .Where(skill => skillIds.Contains(skill.Id))
                .ToListAsync(ct);

            if (skills.Count != skillIds.Count)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "One or more skills were not found.");
            }

            agent.Update(
                name,
                request.Role,
                request.Kind,
                request.Type,
                request.Status,
                request.SystemPrompt,
                request.ModelOverride,
                request.Temperature,
                request.MaxTokens,
                request.MetadataJson);

            foreach (var existingLink in agent.AgentProviders.ToList())
            {
                agentProviderRepo.Delete(existingLink);
            }

            foreach (var existingLink in agent.AgentSkills.ToList())
            {
                agentSkillRepo.Delete(existingLink);
            }

            for (var index = 0; index < providerIds.Count; index++)
            {
                await agentProviderRepo.AddAsync(
                    new AgentProvider(agent.Id, providerIds[index], index, "active"),
                    ct);
            }

            foreach (var skillId in skillIds)
            {
                await agentSkillRepo.AddAsync(
                    new AgentSkill(agent.Id, skillId, "active"),
                    ct);
            }

            await agentRepo.SaveChangesAsync(ct);

            var updated = await agentRepo.Query()
                .AsNoTracking()
                .Include(item => item.AgentProviders)
                .ThenInclude(item => item.Provider)
                .Include(item => item.AgentSkills)
                .FirstAsync(item => item.Id == agentId, ct);

            return ApiResults.Ok(context, updated.Adapt<AgentResponse>(mapper.Config));
        }).RequireAuthorization();

        group.MapDelete("/agents/{agentId:guid}", async (
            Guid agentId,
            IAgentRepo repo,
            CancellationToken ct) =>
        {
            var agent = await repo.FindAsync(agentId, ct);
            if (agent is null)
            {
                return Results.NoContent();
            }

            repo.Delete(agent);
            await repo.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private static List<Guid> ParseEntityIds(IReadOnlyList<string> ids)
    {
        var parsed = new List<Guid>();

        foreach (var id in ids)
        {
            if (Guid.TryParse(id, out var value))
            {
                parsed.Add(value);
            }
        }

        return parsed.Distinct().ToList();
    }
}
