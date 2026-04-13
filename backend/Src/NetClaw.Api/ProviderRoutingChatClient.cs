using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using NetClaw.Application.Models.Llm;
using NetClaw.Application.Services;
using NetClaw.Domains.Entities;
using NetClaw.Infra.Contexts;
using NetClaw.Infra.Extensions;
using Npgsql;
using OpenAI;

namespace NetClaw.Api;

public sealed class ProviderRoutingChatClient(
    ChatModeCatalog modeCatalog,
    IServiceScopeFactory scopeFactory,
    IServiceProvider serviceProvider,
    IHttpContextAccessor httpContextAccessor,
    IGovernanceService governance,
    ITeamAgentOrchestrationService teamAgentOrchestrationService,
    IContextCompactor contextCompactor,
    IMemoryProvider memoryProvider,
    IAgentToolService toolService,
    ILoggerFactory loggerFactory,
    ILogger<ProviderRoutingChatClient> logger)
    : IChatClient
{
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var conversationId = EnsureConversationId(options?.ConversationId);
        var userId = GetCurrentUserId();
        var prepared = await PrepareRequestAsync(messages, conversationId, userId, cancellationToken);
        var preparedOptions = PrepareOptions(prepared.Messages, options, conversationId, prepared.UserMemoryText);
        var target = ResolveConversationTarget(preparedOptions);

        var blockedResponse = TryCreateBlockedResponse(prepared.Messages);
        if (blockedResponse is not null)
        {
            blockedResponse.ConversationId ??= conversationId;
            ScheduleBackgroundTasks(prepared.Messages, blockedResponse, target, userId);
            return blockedResponse;
        }

        var selectedTeamId = TryGetSelectedTeamId(preparedOptions);
        if (Guid.TryParse(selectedTeamId, out var teamId))
        {
            return await GetAndPersistResponseAsync(
                teamAgentOrchestrationService.GetResponseAsync(
                    teamId, prepared.Messages, preparedOptions, cancellationToken),
                prepared.Messages, conversationId, target, userId);
        }

        return await GetAndPersistResponseAsync(
            GetDirectAgentOrClientResponseAsync(prepared.Messages, preparedOptions, cancellationToken),
            prepared.Messages, conversationId, target, userId);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        StreamInternalAsync(messages, options, cancellationToken);

    public void Dispose()
    {
    }

    // -------------------------------------------------------------------------
    // Core pipeline
    // -------------------------------------------------------------------------

    private async IAsyncEnumerable<ChatResponseUpdate> StreamInternalAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var conversationId = EnsureConversationId(options?.ConversationId);
        var userId = GetCurrentUserId();
        var prepared = await PrepareRequestAsync(messages, conversationId, userId, cancellationToken);
        var preparedOptions = PrepareOptions(prepared.Messages, options, conversationId, prepared.UserMemoryText);
        var target = ResolveConversationTarget(preparedOptions);

        var blockedUpdate = TryCreateBlockedUpdate(prepared.Messages);
        if (blockedUpdate is not null)
        {
            blockedUpdate.ConversationId ??= conversationId;
            await foreach (var u in StreamAndPersistAsync(
                ToAsyncEnumerable(blockedUpdate),
                prepared.Messages, conversationId, target, userId, cancellationToken))
            {
                yield return u;
            }

            yield break;
        }

        var selectedTeamId = TryGetSelectedTeamId(preparedOptions);
        IAsyncEnumerable<ChatResponseUpdate> source = Guid.TryParse(selectedTeamId, out var teamId)
            ? teamAgentOrchestrationService.GetStreamingResponseAsync(
                teamId, prepared.Messages, preparedOptions, cancellationToken)
            : GetDirectAgentOrClientStreamingResponseAsync(
                prepared.Messages, preparedOptions, cancellationToken);

        await foreach (var update in StreamAndPersistAsync(
            source, prepared.Messages, conversationId, target, userId, cancellationToken))
        {
            yield return update;
        }
    }

    private async Task<ChatResponse> GetAndPersistResponseAsync(
        Task<ChatResponse> responseTask,
        IReadOnlyList<ChatMessage> messages,
        string conversationId,
        ConversationTarget target,
        Guid? userId)
    {
        var response = await responseTask;
        response.ConversationId ??= conversationId;
        ScheduleBackgroundTasks(messages, response, target, userId);
        return response;
    }

    private async IAsyncEnumerable<ChatResponseUpdate> StreamAndPersistAsync(
        IAsyncEnumerable<ChatResponseUpdate> updates,
        IReadOnlyList<ChatMessage> messages,
        string conversationId,
        ConversationTarget target,
        Guid? userId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffered = new List<ChatResponseUpdate>();

        await foreach (var update in updates.WithCancellation(cancellationToken))
        {
            update.ConversationId ??= conversationId;
            buffered.Add(update);
            yield return update;
        }

        if (buffered.Count == 0)
        {
            yield break;
        }

        var response = buffered.ToChatResponse();
        response.ConversationId ??= conversationId;
        ScheduleBackgroundTasks(messages, response, target, userId);
    }

    // -------------------------------------------------------------------------
    // Message preparation + compaction
    // -------------------------------------------------------------------------

    private async Task<PreparedRequest> PrepareRequestAsync(
        IEnumerable<ChatMessage> messages,
        string conversationId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var incoming = messages.ToList();
        if (incoming.Count == 0)
        {
            return new PreparedRequest(incoming, null);
        }

        if (incoming.Any(message =>
                message.Role == ChatRole.Tool ||
                message.Contents.Any(content => content is FunctionCallContent or ToolCallContent or FunctionResultContent or ToolResultContent)))
        {
            logger.LogWarning(
                "PrepareRequestAsync received incoming tool-related messages conversationId={ConversationId} count={Count} roles={Roles}",
                conversationId,
                incoming.Count,
                incoming.Select(message => message.Role.ToString()).ToArray());
        }

        // Load persisted history
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var conversation = await db.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.ExternalId == conversationId, cancellationToken);

        IReadOnlyList<ChatMessage> allMessages;
        if (conversation is null || conversation.Messages.Count == 0)
        {
            allMessages = incoming;
        }
        else
        {
            var persisted = conversation.Messages
                .OrderBy(m => m.Sequence)
                .ThenBy(m => m.CreatedOn)
                .Select(m =>
                {
                    var msg = new ChatMessage(ParseChatRole(m.Role), m.Content ?? string.Empty);
                    msg.MessageId = m.ExternalMessageId ?? m.Id.ToString();
                    return msg;
                })
                .ToList();

            allMessages = MergeMessages(persisted, incoming);
        }

        // Strip tool-call messages from history: tool results and assistant messages
        // that contain only function calls (no text) must not be replayed as orphaned history.
        // Tool invocations are re-executed server-side fresh each request.
        var sanitized = StripToolMessages(allMessages);

        // Apply compaction: summary + last N + user memories
        var compacted = await contextCompactor.BuildContextAsync(
            conversationId, userId, sanitized, cancellationToken);

        // Build final message list: system messages from incoming + summary + recent
        var systemMessages = incoming.Where(m => m.Role == ChatRole.System).ToList();
        var finalMessages = BuildFinalMessages(systemMessages, compacted);

        return new PreparedRequest(finalMessages, compacted.UserMemoryText);
    }

    private static IReadOnlyList<ChatMessage> BuildFinalMessages(
        IEnumerable<ChatMessage> systemMessages,
        CompactedContext compacted)
    {
        var result = new List<ChatMessage>();
        result.AddRange(systemMessages);

        if (!string.IsNullOrWhiteSpace(compacted.SummaryText))
        {
            result.Add(new ChatMessage(ChatRole.System,
                $"[Earlier conversation summary]\n{compacted.SummaryText}"));
        }

        result.AddRange(compacted.RecentMessages);
        return result;
    }

    // -------------------------------------------------------------------------
    // Background tasks (persist + compact + memory)
    // -------------------------------------------------------------------------

    private void ScheduleBackgroundTasks(
        IEnumerable<ChatMessage> messages,
        ChatResponse response,
        ConversationTarget target,
        Guid? userId)
    {
        var latestUserMessage = GetLatestMessage(messages, ChatRole.User);
        var assistantMessage = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant);
        var conversationId = response.ConversationId;

        if (latestUserMessage is null ||
            assistantMessage is null ||
            string.IsNullOrWhiteSpace(latestUserMessage.Text) ||
            string.IsNullOrWhiteSpace(assistantMessage.Text) ||
            string.IsNullOrWhiteSpace(conversationId))
        {
            return;
        }

        var payload = new ConversationPersistencePayload(
            conversationId,
            target,
            latestUserMessage.MessageId,
            latestUserMessage.Text,
            assistantMessage.MessageId,
            assistantMessage.Text,
            BuildConversationMetadata(response));

        _ = Task.Run(async () =>
        {
            try
            {
                await PersistConversationAsync(payload, CancellationToken.None);
                await contextCompactor.MaybeUpdateSummaryAsync(payload.ConversationId, CancellationToken.None);

                if (userId.HasValue)
                {
                    await memoryProvider.StoreAsync(
                        userId.Value,
                        payload.UserContent,
                        payload.AssistantContent,
                        source: payload.Target.Type,
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background tasks failed for conversation {ConversationId}", payload.ConversationId);
            }
        });
    }

    private async Task PersistConversationAsync(
        ConversationPersistencePayload payload,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await PersistConversationAttemptAsync(payload, attempt, cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "PersistConversationAsync concurrency conflict conversationId={ConversationId} attempt={Attempt}/{MaxAttempts} userMessageId={UserMessageId} assistantMessageId={AssistantMessageId}",
                    payload.ConversationId,
                    attempt,
                    maxAttempts,
                    payload.UserMessageId,
                    payload.AssistantMessageId);
                await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), cancellationToken);
            }
            catch (DbUpdateException ex) when (attempt < maxAttempts && IsRetryablePersistenceException(ex))
            {
                logger.LogWarning(
                    ex,
                    "PersistConversationAsync retryable update failure conversationId={ConversationId} attempt={Attempt}/{MaxAttempts} userMessageId={UserMessageId} assistantMessageId={AssistantMessageId}",
                    payload.ConversationId,
                    attempt,
                    maxAttempts,
                    payload.UserMessageId,
                    payload.AssistantMessageId);
                await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), cancellationToken);
            }
        }

        await PersistConversationAttemptAsync(payload, maxAttempts, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Provider / option resolution (unchanged)
    // -------------------------------------------------------------------------

    private async Task<ChatResponse> GetDirectAgentOrClientResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        var selectedAgentId = TryGetSelectedAgentId(options);
        var agentContext = LoadAgentContext(selectedAgentId);
        if (agentContext is null)
        {
            return await GetFunctionInvokingClient(options).GetResponseAsync(messages, options, cancellationToken);
        }

        var agent = await BuildDirectAgentAsync(agentContext, options?.Instructions, cancellationToken);
        var session = await agent.CreateSessionAsync(cancellationToken);
        var response = await agent.RunAsync(messages, session, options: null, cancellationToken);
        return response.AsChatResponse();
    }

    private async IAsyncEnumerable<ChatResponseUpdate> GetDirectAgentOrClientStreamingResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var selectedAgentId = TryGetSelectedAgentId(options);
        var agentContext = LoadAgentContext(selectedAgentId);
        if (agentContext is null)
        {
            await foreach (var update in GetFunctionInvokingClient(options).GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                yield return update;
            }

            yield break;
        }

        var agent = await BuildDirectAgentAsync(agentContext, options?.Instructions, cancellationToken);
        var session = await agent.CreateSessionAsync(cancellationToken);
        await foreach (var update in agent.RunStreamingAsync(messages, session, options: null, cancellationToken).AsChatResponseUpdatesAsync())
        {
            yield return update;
        }
    }

    private IChatClient ResolveClient(ChatOptions? options)
    {
        var requestedModel = TryGetRequestedModel(options);
        var selectedAgentId = TryGetSelectedAgentId(options);
        var agentContext = LoadAgentContext(selectedAgentId);

        if (agentContext is null)
        {
            throw new Exception("No agent context was found.");
        }

        var selection = ResolveSelection(agentContext, requestedModel);
        var agentProvider = agentContext.Providers[0];

        logger.LogInformation(
            "Routing chat request to provider {Provider} with model {Model}",
            selection.Provider, selection.Model);

        if (agentContext is not null)
        {
            logger.LogInformation(
                "Direct chat target agentId={AgentId} agentName={AgentName} skillCount={SkillCount} skillSlugs={SkillSlugs}",
                agentContext.Id,
                agentContext.Name,
                agentContext.Skills.Count,
                agentContext.Skills.Select(skill => skill.Slug).ToArray());
        }

        var client = new OpenAIClient(
            new ApiKeyCredential(agentProvider.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(agentProvider.BaseUrl) });

        return client.GetChatClient(selection.Model).AsIChatClient();
    }

    private IChatClient GetFunctionInvokingClient(ChatOptions? options) =>
        new ChatClientBuilder(ResolveClient(options))
            .UseFunctionInvocation(loggerFactory)
            .Build();

    private ChatOptions PrepareOptions(
        IEnumerable<ChatMessage> messages,
        ChatOptions? source,
        string conversationId,
        string? userMemoryText)
    {
        var requestedModel = TryGetRequestedModel(source);
        var selectedAgentId = TryGetSelectedAgentId(source);
        var agentContext = LoadAgentContext(selectedAgentId);
        var selection = ResolveSelection(agentContext, requestedModel);
        var requiresExcelTool = !HasToolResult(messages) && DetectExcelIntent(messages);

        var cloned = new ChatOptions
        {
            ConversationId = conversationId,
            Instructions = BuildInstructions(source?.Instructions, agentContext?.SystemPrompt, userMemoryText),
            Tools = source?.Tools,
            Temperature = source?.Temperature,
            MaxOutputTokens = source?.MaxOutputTokens,
            TopP = source?.TopP,
            TopK = source?.TopK,
            FrequencyPenalty = source?.FrequencyPenalty,
            PresencePenalty = source?.PresencePenalty,
            Seed = source?.Seed,
            Reasoning = source?.Reasoning,
            ResponseFormat = source?.ResponseFormat,
            StopSequences = source?.StopSequences,
            AllowMultipleToolCalls = source?.AllowMultipleToolCalls,
            ToolMode = source?.ToolMode,
        };

        cloned.ModelId = selection.Model;
        cloned.AdditionalProperties ??= new AdditionalPropertiesDictionary();

        if (source?.AdditionalProperties is not null)
        {
            foreach (var pair in source.AdditionalProperties)
            {
                cloned.AdditionalProperties[pair.Key] = pair.Value;
            }
        }

        cloned.AdditionalProperties["selected_model"] = selection.Id;
        cloned.AdditionalProperties["provider"] = selection.Provider;
        if (agentContext is not null)
        {
            cloned.AdditionalProperties["selected_agent_id"] = agentContext.Id.ToString();
        }

        if (requiresExcelTool)
        {
            cloned.ToolMode = ChatToolMode.RequireSpecific("create_excel_file");
            cloned.AllowMultipleToolCalls = false;
        }

        return cloned;
    }

    private string BuildInstructions(
        string? requestInstructions,
        string? agentSystemPrompt,
        string? userMemoryText)
    {
        var sections = new List<string> { modeCatalog.GetInstructions() };

        if (!string.IsNullOrWhiteSpace(userMemoryText))
        {
            sections.Add($"User context (remembered facts):\n{userMemoryText}");
        }

        if (!string.IsNullOrWhiteSpace(agentSystemPrompt))
        {
            sections.Add($"Agent-specific instructions:\n{agentSystemPrompt}");
        }

        if (!string.IsNullOrWhiteSpace(requestInstructions))
        {
            sections.Add($"Additional request instructions:\n{requestInstructions}");
        }

        return string.Join("\n\n", sections);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private Guid? GetCurrentUserId()
    {
        var claim = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static IReadOnlyList<ChatMessage> MergeMessages(
        IReadOnlyList<ChatMessage> persisted,
        IReadOnlyList<ChatMessage> incoming)
    {
        var merged = new List<ChatMessage>(persisted.Count + incoming.Count);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        void AddMessages(IEnumerable<ChatMessage> source)
        {
            foreach (var message in source)
            {
                var id = message.MessageId;
                if (!string.IsNullOrWhiteSpace(id) && !seenIds.Add(id))
                {
                    continue;
                }

                var key = $"{message.Role}:{message.Text ?? string.Empty}";
                if (string.IsNullOrWhiteSpace(id) && !seenKeys.Add(key))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(id))
                {
                    seenKeys.Add(key);
                }

                merged.Add(message);
            }
        }

        AddMessages(persisted);
        AddMessages(incoming);
        return merged;
    }

    private ChatResponse? TryCreateBlockedResponse(IEnumerable<ChatMessage> messages)
    {
        var detection = DetectLatestUserInjection(messages);
        return detection is null
            ? null
            : new ChatResponse(new ChatMessage(ChatRole.Assistant, BuildBlockedMessage(detection)));
    }

    private ChatResponseUpdate? TryCreateBlockedUpdate(IEnumerable<ChatMessage> messages)
    {
        var detection = DetectLatestUserInjection(messages);
        return detection is null
            ? null
            : new ChatResponseUpdate(ChatRole.Assistant, BuildBlockedMessage(detection));
    }

    private static string? TryGetRequestedModel(ChatOptions? options)
    {
        if (options?.AdditionalProperties is not null)
        {
            if (options.AdditionalProperties.TryGetValue("selected_model", out var m1) && m1 is not null)
            {
                return m1.ToString();
            }

            if (options.AdditionalProperties.TryGetValue("model", out var m2) && m2 is not null)
            {
                return m2.ToString();
            }
        }

        return options?.ModelId;
    }

    private string? TryGetSelectedAgentId(ChatOptions? options)
    {
        if (options?.AdditionalProperties is not null &&
            options.AdditionalProperties.TryGetValue("selected_agent_id", out var v) && v is not null)
        {
            return v.ToString();
        }

        var query = httpContextAccessor.HttpContext?.Request.Query["agent_id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(query) ? null : query;
    }

    private string? TryGetSelectedTeamId(ChatOptions? options)
    {
        if (options?.AdditionalProperties is not null &&
            options.AdditionalProperties.TryGetValue("selected_team_id", out var v) && v is not null)
        {
            return v.ToString();
        }

        var query = httpContextAccessor.HttpContext?.Request.Query["team_id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(query) ? null : query;
    }

    private ProviderSelection ResolveSelection(AgentRuntimeContext? agentContext, string? requestedModel)
    {
        if (agentContext is not null)
        {
            var p = agentContext.Providers[0];
            var model = p.ModelOverride ?? p.DefaultModel;
            return new ProviderSelection(p.ProviderId.ToString(), $"{p.ProviderId}:{model}", model);
        }

        return ResolveSelectionFromDb(requestedModel);
    }

    private ProviderSelection ResolveSelectionFromDb(string? requestedModel)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var providers = db.Providers
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToList();

        if (providers.Count == 0)
        {
            throw new InvalidOperationException("No AI provider configured.");
        }

        if (string.IsNullOrWhiteSpace(requestedModel))
        {
            var first = providers[0];
            return new ProviderSelection(first.Id.ToString(), $"{first.Id}:{first.DefaultModel}", first.DefaultModel);
        }

        var parts = requestedModel.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new InvalidOperationException($"Invalid model format '{requestedModel}'. Expected 'provider:model'.");
        }

        var matched = providers.FirstOrDefault(p =>
            p.Id.ToString().Equals(parts[0], StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Provider '{parts[0]}' is not configured.");

        if (!matched.DefaultModel.Equals(parts[1], StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Model '{parts[1]}' is not enabled for provider '{matched.Id}'.");
        }

        return new ProviderSelection(matched.Id.ToString(), $"{matched.Id}:{matched.DefaultModel}", matched.DefaultModel);
    }

    private AgentRuntimeContext? LoadAgentContext(string? selectedAgentId)
    {
        if (!Guid.TryParse(selectedAgentId, out var agentId))
        {
            return null;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var agent = db.Agents
            .AsNoTracking()
            .Include(a => a.AgentProviders)
            .ThenInclude(ap => ap.Provider)
            .Include(a => a.AgentSkills)
            .ThenInclude(link => link.Skill)
            .FirstOrDefault(a => a.Id == agentId);

        if (agent is null || agent.AgentProviders.Count == 0)
        {
            return null;
        }

        return new AgentRuntimeContext(
            agent.Id,
            agent.Name,
            agent.SystemPrompt,
            agent.AgentProviders
                .OrderBy(ap => ap.Priority)
                .Select(ap => new AgentRuntimeProvider(
                    ap.ProviderId,
                    ap.Provider.DefaultModel,
                    ap.ModelOverride,
                    ap.Provider.EncryptedApiKey,
                    ap.Provider.BaseUrl ?? GetDefaultBaseUrl(ap.Provider.ProviderType)))
                .ToList(),
            agent.AgentSkills
                .Where(link => link.Status == "active" && link.Skill.Status == "active")
                .Select(link => link.Skill)
                .OrderBy(skill => skill.Name)
                .ToList());
    }

    private async Task<AIAgent> BuildDirectAgentAsync(
        AgentRuntimeContext agentContext,
        string? requestInstructions,
        CancellationToken cancellationToken)
    {
        if (agentContext.Providers.Count == 0)
        {
            throw new InvalidOperationException($"Agent '{agentContext.Id}' does not have an active provider.");
        }

        var primaryProvider = agentContext.Providers[0];
        var model = primaryProvider.ModelOverride ?? primaryProvider.DefaultModel;
        var client = new OpenAIClient(
            new ApiKeyCredential(primaryProvider.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(primaryProvider.BaseUrl),
            });

        var skillsProvider = await CreateDirectSkillsProviderAsync(agentContext, cancellationToken);
        var chatOptions = new ChatOptions
        {
            Instructions = requestInstructions,
            Tools = toolService.GetTools().ToList(),
            ModelId = model,
        };

        logger.LogInformation(
            "Building direct AIAgent agentId={AgentId} agentName={AgentName} model={Model} skillCount={SkillCount} skillSlugs={SkillSlugs}",
            agentContext.Id,
            agentContext.Name,
            model,
            agentContext.Skills.Count,
            agentContext.Skills.Select(skill => skill.Slug).ToArray());

        return new ChatClientBuilder(client.GetChatClient(model).AsIChatClient())
            .UseFunctionInvocation(loggerFactory)
            .BuildAIAgent(
                new ChatClientAgentOptions
                {
                    Id = agentContext.Id.ToString(),
                    Name = agentContext.Name,
                    Description = "direct agent chat",
                    ChatOptions = chatOptions,
                    AIContextProviders = skillsProvider is null ? [] : [skillsProvider],
                },
                loggerFactory,
                serviceProvider);
    }

    private async Task<AgentSkillsProvider?> CreateDirectSkillsProviderAsync(
        AgentRuntimeContext agentContext,
        CancellationToken cancellationToken)
    {
        if (agentContext.Skills.Count == 0)
        {
            logger.LogInformation("CreateDirectSkillsProviderAsync skipped because agentId={AgentId} has no linked skills.", agentContext.Id);
            return null;
        }

        var runtimeSkills = agentContext.Skills
            .Select(skill =>
            {
                var parsed = SkillMarkdownParser.Parse(skill.Content);
                var skillName = SanitizeSkillName(skill.Slug, parsed.Name);
                logger.LogInformation(
                    "Preparing direct runtime skill agentId={AgentId} skillId={SkillId} dbSlug={DbSlug} parsedName={ParsedName} runtimeName={RuntimeName} description={Description}",
                    agentContext.Id,
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

        logger.LogInformation(
            "CreateDirectSkillsProviderAsync created runtime skills agentId={AgentId} count={RuntimeSkillCount}",
            agentContext.Id,
            runtimeSkills.Count);

        await Task.CompletedTask;
        return AgentSkillProviderFactory.Create(runtimeSkills, loggerFactory);
    }

    private static string SanitizeSkillName(string slug, string fallbackName)
    {
        var candidate = string.IsNullOrWhiteSpace(slug) ? fallbackName : slug;
        candidate = candidate.Trim().ToLowerInvariant();
        candidate = System.Text.RegularExpressions.Regex.Replace(candidate, @"[^a-z0-9-]+", "-");
        candidate = System.Text.RegularExpressions.Regex.Replace(candidate, @"-+", "-");
        candidate = candidate.Trim('-');

        return string.IsNullOrWhiteSpace(candidate) ? "skill" : candidate;
    }

    private async Task PersistConversationAttemptAsync(
        ConversationPersistencePayload payload,
        int attempt,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        logger.LogInformation(
            "PersistConversationAsync start conversationId={ConversationId} attempt={Attempt} userMessageId={UserMessageId} assistantMessageId={AssistantMessageId}",
            payload.ConversationId,
            attempt,
            payload.UserMessageId,
            payload.AssistantMessageId);

        var conversation = await dbContext.Conversations
            .FirstOrDefaultAsync(item => item.ExternalId == payload.ConversationId, cancellationToken);

        if (conversation is null)
        {
            conversation = new Conversation(
                payload.ConversationId,
                CreateConversationTitle(payload.UserContent),
                "completed",
                payload.Target.Type,
                payload.Target.Id,
                payload.MetadataJson);
            await dbContext.Conversations.AddAsync(conversation, cancellationToken);

            logger.LogInformation(
                "PersistConversationAsync creating conversation conversationId={ConversationId} entityId={EntityId} attempt={Attempt}",
                payload.ConversationId,
                conversation.Id,
                attempt);
        }

        var userMessageAlreadyExists = !string.IsNullOrWhiteSpace(payload.UserMessageId) &&
            await dbContext.ConversationMessages
                .AsNoTracking()
                .AnyAsync(
                    item => item.ConversationId == conversation.Id && item.ExternalMessageId == payload.UserMessageId,
                    cancellationToken);

        conversation.Touch(
            status: "completed",
            metadataJson: payload.MetadataJson,
            lastMessageOn: now);

        if (userMessageAlreadyExists)
        {
            logger.LogInformation(
                "PersistConversationAsync skipping duplicate user message conversationId={ConversationId} entityId={EntityId} userMessageId={UserMessageId} attempt={Attempt}",
                payload.ConversationId,
                conversation.Id,
                payload.UserMessageId,
                attempt);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var currentMaxSequence = await dbContext.ConversationMessages
            .AsNoTracking()
            .Where(item => item.ConversationId == conversation.Id)
            .MaxAsync(item => (int?)item.Sequence, cancellationToken) ?? 0;

        var nextSequence = currentMaxSequence + 1;
        var userMessage = new ConversationMessage(
            conversation.Id,
            nextSequence,
            "user",
            payload.UserContent,
            payload.UserMessageId);
        var assistantMessage = new ConversationMessage(
            conversation.Id,
            nextSequence + 1,
            "assistant",
            payload.AssistantContent,
            payload.AssistantMessageId);

        await dbContext.ConversationMessages.AddRangeAsync([userMessage, assistantMessage], cancellationToken);

        logger.LogInformation(
            "PersistConversationAsync appending messages conversationId={ConversationId} entityId={EntityId} nextSequence={NextSequence} userEntityId={UserEntityId} assistantEntityId={AssistantEntityId} attempt={Attempt}",
            payload.ConversationId,
            conversation.Id,
            nextSequence,
            userMessage.Id,
            assistantMessage.Id,
            attempt);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "PersistConversationAsync completed conversationId={ConversationId} entityId={EntityId} attempt={Attempt}",
            payload.ConversationId,
            conversation.Id,
            attempt);
    }

    private static bool IsRetryablePersistenceException(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgres &&
        postgres.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.SerializationFailure;

    private static string GetDefaultBaseUrl(string providerType) =>
        providerType.Trim().ToLowerInvariant() switch
        {
            "openai" => "https://api.openai.com/v1",
            "deepseek" => "https://api.deepseek.com/v1",
            "gemini" => "https://generativelanguage.googleapis.com/v1beta/openai/",
            _ => throw new InvalidOperationException($"Provider type '{providerType}' requires a BaseUrl.")
        };

    private static bool DetectExcelIntent(IEnumerable<ChatMessage> messages)
    {
        var text = GetLatestUserText(messages);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var keywords = new[] { "excel", "xlsx", "spreadsheet", "workbook", "downloadable table", "export", "file .xlsx", "csv" };
        return keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<ChatMessage> StripToolMessages(IReadOnlyList<ChatMessage> messages) =>
        messages
            .Where(m => m.Role != ChatRole.Tool)
            .Where(m => !m.Contents.All(c => c is FunctionCallContent or ToolCallContent))
            .ToList();

    private static bool HasToolResult(IEnumerable<ChatMessage> messages) =>
        messages.Any(m => m.Contents.Any(c => c is FunctionResultContent or ToolResultContent));

    private DetectionEnvelope? DetectLatestUserInjection(IEnumerable<ChatMessage> messages)
    {
        var text = GetLatestUserText(messages);
        var detection = governance.DetectPromptInjection(text);
        if (detection is null || !detection.IsInjection)
        {
            return null;
        }

        logger.LogWarning(
            "Blocked prompt injection type={T} threat={L} confidence={C}",
            detection.InjectionType, detection.ThreatLevel, detection.Confidence);

        return new DetectionEnvelope(text!, detection);
    }

    private static string? GetLatestUserText(IEnumerable<ChatMessage> messages) =>
        messages
            .LastOrDefault(m => m.Role == ChatRole.User)?
            .Contents
            .OfType<TextContent>()
            .Select(c => c.Text)
            .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

    private static ChatMessage? GetLatestMessage(IEnumerable<ChatMessage> messages, ChatRole role) =>
        messages.LastOrDefault(m => m.Role == role);

    private static ChatRole ParseChatRole(string role) =>
        role.Trim().ToLowerInvariant() switch
        {
            "assistant" => ChatRole.Assistant,
            "system" => ChatRole.System,
            "tool" => ChatRole.Tool,
            _ => ChatRole.User
        };

    private ConversationTarget ResolveConversationTarget(ChatOptions? options)
    {
        var teamId = TryGetSelectedTeamId(options);
        if (Guid.TryParse(teamId, out var tid))
        {
            return new ConversationTarget("team", tid);
        }

        var agentId = TryGetSelectedAgentId(options);
        if (Guid.TryParse(agentId, out var aid))
        {
            return new ConversationTarget("agent", aid);
        }

        var channelId = httpContextAccessor.HttpContext?.Request.Query["channel_id"].FirstOrDefault();
        return Guid.TryParse(channelId, out var cid)
            ? new ConversationTarget("channel", cid)
            : new ConversationTarget(null, null);
    }

    private static string EnsureConversationId(string? conversationId) =>
        string.IsNullOrWhiteSpace(conversationId) ? Guid.NewGuid().ToString("N") : conversationId.Trim();

    private static string? BuildConversationMetadata(ChatResponse response)
    {
        if (response.AdditionalProperties is null || response.AdditionalProperties.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(
            response.AdditionalProperties.ToDictionary(p => p.Key, p => p.Value?.ToString()));
    }

    private static string CreateConversationTitle(string content)
    {
        var normalized = content.Trim();
        return normalized.Length <= 120 ? normalized : normalized[..117].TrimEnd() + "...";
    }

    private static string BuildBlockedMessage(DetectionEnvelope detection) =>
        $"Request blocked by governance before model execution. " +
        $"Reason: prompt injection detected. " +
        $"Type: {detection.Result.InjectionType}. " +
        $"Threat: {detection.Result.ThreatLevel}. " +
        $"Confidence: {detection.Result.Confidence:0.00}. " +
        $"Explanation: {detection.Result.Explanation}";

    private static async IAsyncEnumerable<ChatResponseUpdate> ToAsyncEnumerable(ChatResponseUpdate update)
    {
        yield return update;
        await Task.CompletedTask;
    }
}

internal sealed record PreparedRequest(IReadOnlyList<ChatMessage> Messages, string? UserMemoryText);
internal sealed record ConversationTarget(string? Type, Guid? Id);
internal sealed record ConversationPersistencePayload(
    string ConversationId,
    ConversationTarget Target,
    string? UserMessageId,
    string UserContent,
    string? AssistantMessageId,
    string AssistantContent,
    string? MetadataJson);
