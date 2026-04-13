using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NetClaw.Domains.Entities;
using NetClaw.Infra.Contexts;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetClaw.Application.Models.Llm;
using NetClaw.Application.Services;

namespace NetClaw.Infra.Services;

public sealed class TeamWorkflowRunner(
    IServiceScopeFactory scopeFactory,
    ILogger<TeamWorkflowRunner> logger) : ITeamWorkflowRunner
{
    public async Task<ChatResponse> ExecuteAsync(
        TeamCompiledWorkflow workflow,
        TeamWorkflowExecutionContext executionContext,
        ChatOptions? options,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var agentRun = new AgentRun(
            "team",
            executionContext.Team.Id,
            options?.ConversationId,
            null,
            "running",
            CreatePreview(executionContext.OriginalRequest),
            metadataJson: JsonSerializer.Serialize(new
            {
                team_id = executionContext.Team.Id,
                team_name = executionContext.Team.Name,
                graph = new
                {
                    nodes = executionContext.Team.Members.Select(item => new
                    {
                        node_id = item.TeamMemberId,
                        agent_id = item.AgentId,
                        name = item.AgentName,
                        role = item.Role,
                    }),
                    edges = executionContext.Team.Members
                        .Where(item => item.ReportsToMemberId.HasValue)
                        .Select(item => new
                    {
                        edge_id = $"{item.TeamMemberId}:{item.ReportsToMemberId}",
                        from_node_id = item.TeamMemberId,
                        to_node_id = item.ReportsToMemberId,
                        edge_type = "reportsTo",
                        label = "reportsTo",
                    }),
                },
            }));
        await dbContext.AgentRuns.AddAsync(agentRun, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Running team workflow team={TeamId} orchestrator={OrchestratorAgentId} writer={WriterAgentId} reviewer={ReviewerAgentId} rewriteLoop={RewriteLoop}",
            executionContext.Team.Id,
            workflow.Orchestrator.AgentId,
            workflow.Writer.AgentId,
            workflow.Reviewer.AgentId,
            workflow.SupportsRewriteLoop);

        var writerStep = await StartStepAsync(
            dbContext,
            agentRun.Id,
            workflow.Writer,
            "writer",
            "agent_execution",
            1,
            executionContext.OriginalRequest,
            cancellationToken);
        var writerDraft = await RunAgentAsync(
            executionContext.Agents[workflow.Writer.AgentId],
            CreateWriterMessages(executionContext.Team, workflow.Orchestrator, workflow.Writer, workflow.Reviewer, executionContext.OriginalRequest),
            cancellationToken);
        writerStep.Complete("completed", CreatePreview(writerDraft));
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Team workflow writer output team={TeamId} agent={AgentId} preview={Preview}",
            executionContext.Team.Id,
            workflow.Writer.AgentId,
            CreatePreview(writerDraft));

        var reviewerStep = await StartStepAsync(
            dbContext,
            agentRun.Id,
            workflow.Reviewer,
            "reviewer",
            "agent_execution",
            2,
            writerDraft,
            cancellationToken);
        var review = await RunAgentAsync(
            executionContext.Agents[workflow.Reviewer.AgentId],
            CreateReviewerMessages(executionContext.Team, workflow.Writer, workflow.Reviewer, executionContext.OriginalRequest, writerDraft),
            cancellationToken);
        reviewerStep.Complete(
            IsApprovedReview(review) ? "approved" : "changes_requested",
            CreatePreview(review));
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Team workflow reviewer output team={TeamId} agent={AgentId} approved={Approved} preview={Preview}",
            executionContext.Team.Id,
            workflow.Reviewer.AgentId,
            IsApprovedReview(review),
            CreatePreview(review));

        string finalDraft = writerDraft;
        if (!IsApprovedReview(review) && workflow.SupportsRewriteLoop)
        {
            var rewriteStep = await StartStepAsync(
                dbContext,
                agentRun.Id,
                workflow.Writer,
                "rewrite",
                "agent_execution",
                3,
                review,
                cancellationToken);
            finalDraft = await RunAgentAsync(
                executionContext.Agents[workflow.Writer.AgentId],
                CreateRewriteMessages(executionContext.Team, workflow.Writer, workflow.Reviewer, executionContext.OriginalRequest, writerDraft, review),
                cancellationToken);
            rewriteStep.Complete("completed", CreatePreview(finalDraft));
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Team workflow rewrite output team={TeamId} agent={AgentId} preview={Preview}",
                executionContext.Team.Id,
                workflow.Writer.AgentId,
                CreatePreview(finalDraft));

            var secondReviewStep = await StartStepAsync(
                dbContext,
                agentRun.Id,
                workflow.Reviewer,
                "reviewer_recheck",
                "agent_execution",
                4,
                finalDraft,
                cancellationToken);
            review = await RunAgentAsync(
                executionContext.Agents[workflow.Reviewer.AgentId],
                CreateReviewerMessages(executionContext.Team, workflow.Writer, workflow.Reviewer, executionContext.OriginalRequest, finalDraft),
                cancellationToken);
            secondReviewStep.Complete(
                IsApprovedReview(review) ? "approved" : "changes_requested",
                CreatePreview(review));
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Team workflow second review output team={TeamId} agent={AgentId} approved={Approved} preview={Preview}",
                executionContext.Team.Id,
                workflow.Reviewer.AgentId,
                IsApprovedReview(review),
                CreatePreview(review));
        }

        var finalizerStep = await StartStepAsync(
            dbContext,
            agentRun.Id,
            workflow.Orchestrator,
            "finalizer",
            "agent_execution",
            workflow.SupportsRewriteLoop && !IsApprovedReview(review) ? 5 : 3,
            finalDraft,
            cancellationToken);
        var finalResponse = await RunAgentAsync(
            executionContext.Agents[workflow.Orchestrator.AgentId],
            CreateOrchestratorMessages(executionContext.Team, executionContext.OriginalRequest, finalDraft, review),
            cancellationToken);
        finalizerStep.Complete("completed", CreatePreview(finalResponse));
        agentRun.Complete("completed", CreatePreview(finalResponse));
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Team workflow final output team={TeamId} agent={AgentId} preview={Preview}",
            executionContext.Team.Id,
            workflow.Orchestrator.AgentId,
            CreatePreview(finalResponse));

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, finalResponse))
        {
            ConversationId = options?.ConversationId,
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["selected_team_id"] = executionContext.Team.Id.ToString(),
                ["orchestration_agent_id"] = workflow.Orchestrator.AgentId.ToString(),
                ["writer_agent_id"] = workflow.Writer.AgentId.ToString(),
                ["reviewer_agent_id"] = workflow.Reviewer.AgentId.ToString(),
                ["rewrite_loop_enabled"] = workflow.SupportsRewriteLoop,
                ["run_id"] = agentRun.Id.ToString(),
            },
        };
    }

    private static async Task<AgentRunStep> StartStepAsync(
        AppDbContext dbContext,
        Guid agentRunId,
        TeamMemberRuntimeContext member,
        string stepKey,
        string stepType,
        int sequence,
        string? inputPreview,
        CancellationToken cancellationToken)
    {
        var step = new AgentRunStep(
            agentRunId,
            member.TeamMemberId,
            member.AgentId,
            stepKey,
            stepType,
            sequence,
            "running",
            CreatePreview(inputPreview));
        await dbContext.AgentRunSteps.AddAsync(step, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return step;
    }

    private static IEnumerable<ChatMessage> CreateWriterMessages(
        TeamRuntimeContext team,
        TeamMemberRuntimeContext orchestrationMember,
        TeamMemberRuntimeContext writerMember,
        TeamMemberRuntimeContext reviewerMember,
        string originalRequest)
    {
        return
        [
            new ChatMessage(ChatRole.System, $$"""
                Team: {{team.Name}}
                Coordinator: {{orchestrationMember.AgentName}}
                Your role: {{writerMember.Role ?? "writer"}}
                Reviewer role: {{reviewerMember.Role ?? "reviewer"}}

                Write the initial draft that best satisfies the user request.
                Return only the draft content. Do not add commentary about your process.
                """),
            new ChatMessage(ChatRole.User, originalRequest),
        ];
    }

    private static IEnumerable<ChatMessage> CreateReviewerMessages(
        TeamRuntimeContext team,
        TeamMemberRuntimeContext writerMember,
        TeamMemberRuntimeContext reviewerMember,
        string originalRequest,
        string writerDraft)
    {
        return
        [
            new ChatMessage(ChatRole.System, $$"""
                Team: {{team.Name}}
                Writer: {{writerMember.AgentName}}
                Your role: {{reviewerMember.Role ?? "reviewer"}}

                Review the writer draft against the original request.
                Your response must start with exactly one of:
                APPROVED: yes
                APPROVED: no

                After that, provide concise review feedback. If approval is no, explain what must change.
                """),
            new ChatMessage(ChatRole.User, $$"""
                Original request:
                {{originalRequest}}

                Writer draft:
                {{writerDraft}}
                """),
        ];
    }

    private static IEnumerable<ChatMessage> CreateRewriteMessages(
        TeamRuntimeContext team,
        TeamMemberRuntimeContext writerMember,
        TeamMemberRuntimeContext reviewerMember,
        string originalRequest,
        string writerDraft,
        string review)
    {
        return
        [
            new ChatMessage(ChatRole.System, $$"""
                Team: {{team.Name}}
                Your role: {{writerMember.Role ?? "writer"}}
                Reviewer: {{reviewerMember.AgentName}}

                Revise the draft based on reviewer feedback.
                Return only the improved draft content.
                """),
            new ChatMessage(ChatRole.User, $$"""
                Original request:
                {{originalRequest}}

                Previous draft:
                {{writerDraft}}

                Reviewer feedback:
                {{review}}
                """),
        ];
    }

    private static IEnumerable<ChatMessage> CreateOrchestratorMessages(
        TeamRuntimeContext team,
        string originalRequest,
        string finalDraft,
        string review)
    {
        return
        [
            new ChatMessage(ChatRole.System, $$"""
                Team: {{team.Name}}
                You are the orchestration agent responsible for returning the final user-facing response.

                Use the final draft and the latest review result to produce the answer.
                Return the final content only. Do not mention internal team workflow unless the user asked for it.
                """),
            new ChatMessage(ChatRole.User, $$"""
                Original request:
                {{originalRequest}}

                Final draft:
                {{finalDraft}}

                Latest review:
                {{review}}
                """),
        ];
    }

    private static async Task<string> RunAgentAsync(
        AIAgent agent,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var session = await agent.CreateSessionAsync(cancellationToken);
        var response = await agent.RunAsync(messages, session, options: null, cancellationToken);
        return response.AsChatResponse().Text;
    }

    private static bool IsApprovedReview(string review) =>
        review.Contains("APPROVED: yes", StringComparison.OrdinalIgnoreCase) ||
        review.Contains("APPROVED:true", StringComparison.OrdinalIgnoreCase) ||
        review.Contains("APPROVED: true", StringComparison.OrdinalIgnoreCase);

    private static string CreatePreview(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "<empty>";
        }

        const int maxLength = 400;
        var normalized = value.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized[..maxLength]}...";
    }
}
