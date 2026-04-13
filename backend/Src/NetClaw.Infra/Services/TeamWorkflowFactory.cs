using NetClaw.Application.Models.Llm;
using NetClaw.Application.Services;

namespace NetClaw.Infra.Services;

public sealed class TeamWorkflowFactory : ITeamWorkflowFactory
{
    public TeamCompiledWorkflow Create(TeamRuntimeContext team)
    {
        var orderedMembers = team.Members
            .OrderBy(item => item.Order)
            .ThenBy(item => item.AgentName)
            .ToList();
        var roots = orderedMembers
            .Where(item => item.ReportsToMemberId is null)
            .ToList();

        if (roots.Count == 0)
        {
            throw new InvalidOperationException(
                $"Agent team '{team.Name}' must contain at least one root member.");
        }

        var orchestrator = roots[0];
        var workers = orderedMembers
            .Where(item => item.TeamMemberId != orchestrator.TeamMemberId)
            .ToList();

        if (workers.Count < 2)
        {
            throw new InvalidOperationException(
                $"Agent team '{team.Name}' must contain at least three members to build writer/reviewer flow.");
        }

        var writer = workers
            .OrderBy(item => ScoreRole(item.Role, ["writer", "author", "draft", "dev", "developer"]))
            .ThenBy(item => item.Order)
            .ThenBy(item => item.AgentName)
            .First();

        var reviewerCandidates = workers
            .Where(item => item.AgentId != writer.AgentId)
            .ToList();

        if (reviewerCandidates.Count == 0)
        {
            throw new InvalidOperationException(
                $"Agent team '{team.Name}' must contain at least two distinct worker agents.");
        }

        var preferredReviewer = reviewerCandidates
            .Where(item => item.TeamMemberId == writer.ReportsToMemberId)
            .OrderBy(item => item.Order)
            .FirstOrDefault();

        var reviewer = preferredReviewer
            ?? reviewerCandidates
                .OrderBy(item => ScoreRole(item.Role, ["review", "reviewer", "critic", "qa", "lead", "manager"]))
                .ThenBy(item => item.Order)
                .ThenBy(item => item.AgentName)
                .First();

        return new TeamCompiledWorkflow(
            orchestrator,
            writer,
            reviewer,
            writer.ReportsToMemberId == reviewer.TeamMemberId || reviewer.ReportsToMemberId == writer.TeamMemberId);
    }

    private static int ScoreRole(string? role, IReadOnlyList<string> preferredPatterns)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return 1000;
        }

        for (var index = 0; index < preferredPatterns.Count; index++)
        {
            if (role.Contains(preferredPatterns[index], StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return 100;
    }
}
