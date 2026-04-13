using System.ClientModel;
using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetClaw.Application.Models.Llm;
using NetClaw.Application.Services;
using NetClaw.Infra.Contexts;
using NetClaw.Infra.Extensions;
using OpenAI;
using RuntimeAgentSkill = Microsoft.Agents.AI.AgentSkill;

namespace NetClaw.Infra.Services;

public sealed class TeamAgentOrchestrationService(
    ChatModeCatalog modeCatalog,
    IServiceScopeFactory scopeFactory,
    IAgentToolService toolService,
    ILoggerFactory loggerFactory,
    IServiceProvider serviceProvider,
    ITeamWorkflowFactory workflowFactory,
    ITeamWorkflowRunner workflowRunner) : ITeamAgentOrchestrationService
{
    private readonly ILogger<TeamAgentOrchestrationService> _logger = loggerFactory.CreateLogger<TeamAgentOrchestrationService>();

    public async Task<ChatResponse> GetResponseAsync(
        Guid teamId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var teamContext = await LoadTeamContextAsync(teamId, cancellationToken);
        var workflow = workflowFactory.Create(teamContext);
        var originalRequest = GetLatestUserText(messages)
            ?? throw new InvalidOperationException("Team orchestration requires a user message.");

        var agentMap = new Dictionary<Guid, AIAgent>
        {
            [workflow.Orchestrator.AgentId] = await BuildAgentAsync(workflow.Orchestrator, options?.Instructions, cancellationToken),
            [workflow.Writer.AgentId] = await BuildAgentAsync(workflow.Writer, options?.Instructions, cancellationToken),
            [workflow.Reviewer.AgentId] = await BuildAgentAsync(workflow.Reviewer, options?.Instructions, cancellationToken),
        };

        return await workflowRunner.ExecuteAsync(
            workflow,
            new TeamWorkflowExecutionContext(teamContext, originalRequest, agentMap),
            options,
            cancellationToken);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        Guid teamId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(teamId, messages, options, cancellationToken);
        foreach (var update in response.ToChatResponseUpdates())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }
    }

    private async Task<TeamRuntimeContext> LoadTeamContextAsync(Guid teamId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var team = await dbContext.AgentTeams
            .AsNoTracking()
            .Include(item => item.Members)
            .ThenInclude(item => item.Agent!)
            .ThenInclude(item => item.AgentProviders)
            .ThenInclude(item => item.Provider)
            .Include(item => item.Members)
            .ThenInclude(item => item.Agent!)
            .ThenInclude(item => item.AgentSkills)
            .ThenInclude(item => item.Skill)
            .FirstOrDefaultAsync(item => item.Id == teamId && item.Status == "active", cancellationToken)
            ?? throw new InvalidOperationException($"Agent team '{teamId}' was not found or is not active.");

        var members = team.Members
            .Where(item => item.Status == "active")
            .OrderBy(item => item.Order)
            .ThenBy(item => item.CreatedOn)
            .Where(item => item.Agent is not null)
            .Select(item =>
            {
                var agent = item.Agent!;
                var providers = agent.AgentProviders
                    .Where(link => link.Status == "active" && link.Provider.IsActive)
                    .OrderBy(link => link.Priority)
                    .Select(link => new AgentRuntimeProvider(
                        link.ProviderId,
                        link.Provider.DefaultModel,
                        link.ModelOverride,
                        link.Provider.EncryptedApiKey,
                        link.Provider.BaseUrl ?? GetDefaultBaseUrl(link.Provider.ProviderType)))
                    .ToList();

                return new TeamMemberRuntimeContext(
                    item.Id,
                    item.AgentId,
                    agent.Name,
                    item.Role,
                    item.Order,
                    item.ReportsToMemberId,
                    agent.SystemPrompt,
                    providers,
                    agent.AgentSkills
                        .Where(link => link.Status == "active" && link.Skill.Status == "active")
                        .Select(link => link.Skill)
                        .ToList());
            })
            .ToList();

        if (members.Count == 0)
        {
            throw new InvalidOperationException($"Agent team '{team.Name}' does not have any active members.");
        }

        _logger.LogInformation(
            "Loaded team context teamId={TeamId} teamName={TeamName} memberCount={MemberCount} members={Members}",
            team.Id,
            team.Name,
            members.Count,
            members.Select(member => new
            {
                member.AgentId,
                member.AgentName,
                skillCount = member.Skills.Count,
                skillSlugs = member.Skills.Select(skill => skill.Slug).ToArray(),
            }).ToArray());

        return new TeamRuntimeContext(team.Id, team.Name, team.Description, team.MetadataJson, members);
    }

    private async Task<AIAgent> BuildAgentAsync(
        TeamMemberRuntimeContext member,
        string? requestInstructions,
        CancellationToken cancellationToken)
    {
        if (member.Providers.Count == 0)
        {
            throw new InvalidOperationException($"Agent '{member.AgentName}' does not have an active provider.");
        }

        var primaryProvider = member.Providers[0];
        var model = primaryProvider.ModelOverride ?? primaryProvider.DefaultModel;
        _logger.LogInformation(
            "Building team agent agentId={AgentId} agentName={AgentName} model={Model} skillCount={SkillCount} skillSlugs={SkillSlugs}",
            member.AgentId,
            member.AgentName,
            model,
            member.Skills.Count,
            member.Skills.Select(skill => skill.Slug).ToArray());

        var client = new OpenAIClient(
            new ApiKeyCredential(primaryProvider.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(primaryProvider.BaseUrl),
            });

        var skillsProvider = await CreateSkillsProviderAsync(member.Skills, cancellationToken);
        var chatOptions = new ChatOptions
        {
            Instructions = BuildMemberInstructions(member, requestInstructions),
            Tools = toolService.GetTools().ToList(),
            ModelId = model,
        };

        return new ChatClientBuilder(client.GetChatClient(model).AsIChatClient())
            .UseFunctionInvocation(loggerFactory)
            .BuildAIAgent(
                new ChatClientAgentOptions
                {
                    Id = member.AgentId.ToString(),
                    Name = member.AgentName,
                    Description = member.Role ?? "team member",
                    ChatOptions = chatOptions,
                    AIContextProviders = skillsProvider is null ? [] : [skillsProvider],
                },
                loggerFactory,
                serviceProvider);
    }

    private async Task<AgentSkillsProvider?> CreateSkillsProviderAsync(
        IReadOnlyList<NetClaw.Domains.Entities.Skill> skills,
        CancellationToken cancellationToken)
    {
        if (skills.Count == 0)
        {
            _logger.LogInformation("CreateSkillsProviderAsync skipped because no team skills were attached.");
            return null;
        }

        var runtimeSkills = skills
            .Select(skill =>
            {
                var parsed = SkillMarkdownParser.Parse(skill.Content);
                var skillName = SanitizeSkillName(skill.Slug, parsed.Name);
                _logger.LogInformation(
                    "Preparing team runtime skill skillId={SkillId} dbSlug={DbSlug} parsedName={ParsedName} runtimeName={RuntimeName} description={Description}",
                    skill.Id,
                    skill.Slug,
                    parsed.Name,
                    skillName,
                    parsed.Description);

                return new AgentInlineSkill(
                    skillName,
                    parsed.Description,
                    parsed.Instructions,
                    parsed.License,
                    parsed.Compatibility,
                    parsed.AllowedTools,
                    parsed.Metadata);
            })
            .ToList();

        _logger.LogInformation("CreateSkillsProviderAsync created runtime skills count={RuntimeSkillCount}", runtimeSkills.Count);

        await Task.CompletedTask;
        return AgentSkillProviderFactory.Create(runtimeSkills, loggerFactory);
    }

    private string BuildMemberInstructions(
        TeamMemberRuntimeContext member,
        string? requestInstructions)
    {
        var sections = new List<string>
        {
            modeCatalog.GetInstructions(),
            "You are participating in an agent team.",
        };

        if (!string.IsNullOrWhiteSpace(member.Role))
        {
            sections.Add($"Your team role is '{member.Role}'.");
        }

        if (!string.IsNullOrWhiteSpace(member.SystemPrompt))
        {
            sections.Add($"Agent-specific instructions:\n{member.SystemPrompt}");
        }

        if (!string.IsNullOrWhiteSpace(requestInstructions))
        {
            sections.Add($"Additional request instructions:\n{requestInstructions}");
        }

        return string.Join("\n\n", sections);
    }

    private static string? GetLatestUserText(IEnumerable<ChatMessage> messages) =>
        messages
            .LastOrDefault(message => message.Role == ChatRole.User)?
            .Contents
            .OfType<TextContent>()
            .Select(content => content.Text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

    private static string GetDefaultBaseUrl(string providerType) =>
        providerType.Trim().ToLowerInvariant() switch
        {
            "openai" => "https://api.openai.com/v1",
            "deepseek" => "https://api.deepseek.com/v1",
            "gemini" => "https://generativelanguage.googleapis.com/v1beta/openai/",
            _ => throw new InvalidOperationException($"Provider type '{providerType}' requires a base URL to be configured."),
        };

    private static string SanitizeSkillName(string slug, string fallbackName)
    {
        var candidate = string.IsNullOrWhiteSpace(slug) ? fallbackName : slug;
        candidate = candidate.Trim().ToLowerInvariant();
        candidate = Regex.Replace(candidate, @"[^a-z0-9-]+", "-");
        candidate = Regex.Replace(candidate, @"-+", "-");
        candidate = candidate.Trim('-');

        return string.IsNullOrWhiteSpace(candidate) ? "skill" : candidate;
    }
}
