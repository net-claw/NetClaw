using Microsoft.EntityFrameworkCore;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Application.Mappers;
using NetClaw.Application.Services;
using NetClaw.Contracts.Governance;
using NetClaw.Domains.Entities;
using NetClaw.Infra.Contexts;

namespace NetClaw.Api.Endpoints;

public sealed class GovernanceSettingsEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/governance/evaluate", (
            IGovernanceService governance,
            string toolName,
            string? input) =>
        {
            var args = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(input))
            {
                args["input"] = input;
            }

            var result = governance.EvaluateToolCall(toolName, args);

            return Results.Ok(new
            {
                tool_name = toolName,
                input,
                allowed = result.Allowed,
                reason = result.Reason,
                action = result.Action,
                matched_rule = result.MatchedRule,
                policy_path = governance.PolicyPath,
            });
        }).RequireAuthorization();

        group.MapGet("/governance/settings/global", async (
            HttpContext context,
            AppDbContext dbContext,
            CancellationToken ct) =>
        {
            var setting = await dbContext.GovernanceSettings
                .AsNoTracking()
                .Where(item => item.ScopeType == GovernanceScopeType.Global && item.ScopeId == null)
                .OrderByDescending(item => item.CreatedOn)
                .FirstOrDefaultAsync(ct);

            return ApiResults.Ok(
                context,
                (setting ?? new GovernanceSetting(
                    GovernanceScopeType.Global,
                    null,
                    createdBy: "System")).ToResponse());
        }).RequireAuthorization();

        group.MapPut("/governance/settings/global", async (
            UpdateGovernanceSettingRequest request,
            HttpContext context,
            AppDbContext dbContext,
            IGovernanceService governanceService,
            CancellationToken ct) =>
        {
            var userName = context.User.Identity?.Name ?? "System";

            var setting = await dbContext.GovernanceSettings
                .Where(item => item.ScopeType == GovernanceScopeType.Global && item.ScopeId == null)
                .OrderByDescending(item => item.CreatedOn)
                .FirstOrDefaultAsync(ct);

            if (setting is null)
            {
                setting = new GovernanceSetting(
                    GovernanceScopeType.Global,
                    null,
                    request.Enable_Builtin_Prompt_Injection,
                    request.Enable_Custom_Prompt_Injection,
                    request.Enable_Audit,
                    request.Enable_Metrics,
                    request.Enable_Circuit_Breaker,
                    request.Builtin_Detector_Config,
                    request.Is_Active,
                    userName);

                await dbContext.GovernanceSettings.AddAsync(setting, ct);
            }
            else
            {
                setting.Update(
                    request.Enable_Builtin_Prompt_Injection,
                    request.Enable_Custom_Prompt_Injection,
                    request.Enable_Audit,
                    request.Enable_Metrics,
                    request.Enable_Circuit_Breaker,
                    request.Builtin_Detector_Config,
                    request.Is_Active,
                    userName);
            }

            await dbContext.SaveChangesAsync(ct);
            governanceService.InvalidateSettingsCache();

            return ApiResults.Ok(context, setting.ToResponse());
        }).RequireAuthorization();
    }
}
