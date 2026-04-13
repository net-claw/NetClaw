using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;

namespace NetClaw.Infra.Extensions;

public static class AgentSkillProviderFactory
{
    public static AgentSkillsProvider Create(
        IEnumerable<AgentInlineSkill> skills,
        ILoggerFactory loggerFactory)
    {
        var materialized = skills.ToList();
        loggerFactory.CreateLogger("NetClaw.AgentSkills")
            .LogInformation(
                "Creating AgentSkillsProvider from inline skills count={SkillCount} disableCaching={DisableCaching} names={SkillNames}",
                materialized.Count,
                true,
                materialized.Select(skill => skill.Frontmatter.Name).ToArray());

        return CreateProvider(materialized, loggerFactory);
    }

    public static AgentSkillsProvider Create(
        AgentSkillsSource source,
        ILoggerFactory loggerFactory)
    {
        loggerFactory.CreateLogger("NetClaw.AgentSkills")
            .LogInformation("Creating AgentSkillsProvider sourceType={SourceType} disableCaching={DisableCaching}", source.GetType().Name, true);

        return CreateProvider(source, loggerFactory);
    }

    private static AgentSkillsProvider CreateProvider(
        IEnumerable<AgentInlineSkill> skills,
        ILoggerFactory loggerFactory)
        => new(
            skills,
            CreateOptions(),
            loggerFactory);

    private static AgentSkillsProvider CreateProvider(
        AgentSkillsSource source,
        ILoggerFactory loggerFactory)
        => new(
            source,
            CreateOptions(),
            loggerFactory);

    private static AgentSkillsProviderOptions CreateOptions() =>
        new()
        {
            DisableCaching = true,
            SkillsInstructionPrompt =
                """
                You have agent skills available. The list below contains discovery summaries only:
                {skills}

                Rules:
                - Treat the summary list as routing metadata, not executable instructions.
                - If the user's task matches a skill, you must call `load_skill` before using that skill.
                - Do not assume the summary contains the full procedure.
                - After loading a skill, follow the loaded SKILL.md instructions exactly.
                - Use `read_skill_resource` only when the loaded skill points to an extra file you need.
                - Use `run_skill_script` only when the loaded skill includes a script and execution is necessary for the task.

                If no skill matches, continue normally with the regular tools.
                {resource_instructions}
                {script_instructions}
                """
        };
}
