using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetClaw.Domains.Entities.Identity;
using NetClaw.Domains.Entities;

namespace NetClaw.Infra.Contexts;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();
    public DbSet<AgentRunStep> AgentRunSteps => Set<AgentRunStep>();
    public DbSet<AgentTeam> AgentTeams => Set<AgentTeam>();
    public DbSet<AgentTeamMember> AgentTeamMembers => Set<AgentTeamMember>();
    public DbSet<AgentProvider> AgentProviders => Set<AgentProvider>();
    public DbSet<AgentSkill> AgentSkills => Set<AgentSkill>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<ConversationSummary> ConversationSummaries => Set<ConversationSummary>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<UserMemory> UserMemories => Set<UserMemory>();
    public DbSet<GovernanceSetting> GovernanceSettings => Set<GovernanceSetting>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Skill> Skills => Set<Skill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(user => user.FirstName).HasMaxLength(200);
            entity.Property(user => user.LastName).HasMaxLength(200);
            entity.Property(user => user.Nickname).HasMaxLength(200);
            entity.Property(user => user.Address).HasMaxLength(1000);
            entity.Property(user => user.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.Property(role => role.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.Property(skill => skill.Name).HasMaxLength(200);
            entity.Property(skill => skill.Slug).HasMaxLength(200);
            entity.Property(skill => skill.Description).HasMaxLength(2000);
            entity.Property(skill => skill.FileName).HasMaxLength(255);
            entity.Property(skill => skill.Status).HasMaxLength(50);
            entity.Property(skill => skill.ArchiveFileName).HasMaxLength(255);
            entity.HasIndex(skill => skill.Slug).IsUnique();
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.Property(provider => provider.Name).HasMaxLength(200);
            entity.Property(provider => provider.ProviderType).HasMaxLength(100);
            entity.Property(provider => provider.DefaultModel).HasMaxLength(200);
            entity.Property(provider => provider.BaseUrl).HasMaxLength(1000);
            entity.Property(provider => provider.EncryptedApiKey).HasMaxLength(4000);
            entity.HasIndex(provider => provider.Name).IsUnique();
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.ToTable("channels");
            entity.Property(channel => channel.Id).HasColumnName("id");
            entity.Property(channel => channel.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(channel => channel.Kind).HasColumnName("kind").HasMaxLength(100);
            entity.Property(channel => channel.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(channel => channel.EncryptedCredentials).HasColumnName("encrypted_credentials").HasMaxLength(8000);
            entity.Property(channel => channel.SettingsJson).HasColumnName("settings_json").HasColumnType("jsonb");
            entity.Property(channel => channel.CreatedBy).HasColumnName("created_by");
            entity.Property(channel => channel.CreatedOn).HasColumnName("created_at");
            entity.Property(channel => channel.UpdatedBy).HasColumnName("updated_by");
            entity.Property(channel => channel.UpdatedOn).HasColumnName("updated_at");
            entity.Property(channel => channel.DeletedAt).HasColumnName("deleted_at");
            entity.HasIndex(channel => channel.Name).IsUnique();
            entity.HasIndex(channel => new { channel.Kind, channel.Status });
        });

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.Property(agent => agent.Name).HasMaxLength(200);
            entity.Property(agent => agent.Role).HasMaxLength(200);
            entity.Property(agent => agent.Kind).HasMaxLength(100);
            entity.Property(agent => agent.Type).HasMaxLength(100);
            entity.Property(agent => agent.Status).HasMaxLength(50);
            entity.Property(agent => agent.ModelOverride).HasMaxLength(200);
            entity.HasIndex(agent => agent.Name).IsUnique();
        });

        modelBuilder.Entity<AgentTeam>(entity =>
        {
            entity.Property(team => team.Name).HasMaxLength(200);
            entity.Property(team => team.Description).HasMaxLength(2000);
            entity.Property(team => team.Status).HasMaxLength(50);
            entity.Property(team => team.MetadataJson).HasColumnType("jsonb");
            entity.HasIndex(team => team.Name).IsUnique();
        });

        modelBuilder.Entity<AgentTeamMember>(entity =>
        {
            entity.Property(member => member.Role).HasMaxLength(200);
            entity.Property(member => member.Status).HasMaxLength(50);
            entity.Property(member => member.MetadataJson).HasColumnType("jsonb");
            entity.HasIndex(member => new { member.AgentTeamId, member.AgentId }).IsUnique();
            entity.HasOne(member => member.AgentTeam)
                .WithMany(team => team.Members)
                .HasForeignKey(member => member.AgentTeamId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(member => member.Agent)
                .WithMany(agent => agent.AgentTeamMembers)
                .HasForeignKey(member => member.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(member => member.ReportsToMember)
                .WithMany(member => member.DirectReports)
                .HasForeignKey(member => member.ReportsToMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgentRun>(entity =>
        {
            entity.Property(run => run.TargetType).HasMaxLength(50);
            entity.Property(run => run.ConversationId).HasMaxLength(200);
            entity.Property(run => run.MessageId).HasMaxLength(200);
            entity.Property(run => run.Status).HasMaxLength(50);
            entity.Property(run => run.InputPreview).HasMaxLength(4000);
            entity.Property(run => run.OutputPreview).HasMaxLength(4000);
            entity.Property(run => run.MetadataJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<AgentRunStep>(entity =>
        {
            entity.Property(step => step.StepKey).HasMaxLength(100);
            entity.Property(step => step.StepType).HasMaxLength(100);
            entity.Property(step => step.Status).HasMaxLength(50);
            entity.Property(step => step.InputPreview).HasMaxLength(4000);
            entity.Property(step => step.OutputPreview).HasMaxLength(4000);
            entity.Property(step => step.MetadataJson).HasColumnType("jsonb");
            entity.HasIndex(step => new { step.AgentRunId, step.Sequence }).IsUnique();
            entity.HasOne(step => step.AgentRun)
                .WithMany(run => run.Steps)
                .HasForeignKey(step => step.AgentRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.Property(conversation => conversation.ExternalId).HasMaxLength(200);
            entity.Property(conversation => conversation.Title).HasMaxLength(500);
            entity.Property(conversation => conversation.Status).HasMaxLength(50);
            entity.Property(conversation => conversation.TargetType).HasMaxLength(50);
            entity.Property(conversation => conversation.MetadataJson).HasColumnType("jsonb");
            entity.HasIndex(conversation => conversation.ExternalId).IsUnique();
            entity.HasIndex(conversation => new { conversation.TargetType, conversation.TargetId });
        });

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.Property(message => message.Role).HasMaxLength(50);
            entity.Property(message => message.Content).HasColumnType("text");
            entity.Property(message => message.ExternalMessageId).HasMaxLength(200);
            entity.Property(message => message.MetadataJson).HasColumnType("jsonb");
            entity.HasIndex(message => new { message.ConversationId, message.Sequence }).IsUnique();
            entity.HasOne(message => message.Conversation)
                .WithMany(conversation => conversation.Messages)
                .HasForeignKey(message => message.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConversationSummary>(entity =>
        {
            entity.Property(s => s.SummaryText).HasColumnType("text");
            entity.HasIndex(s => s.ConversationId).IsUnique();
            entity.HasOne(s => s.Conversation)
                .WithOne()
                .HasForeignKey<ConversationSummary>(s => s.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserMemory>(entity =>
        {
            entity.Property(m => m.Key).HasMaxLength(200);
            entity.Property(m => m.Value).HasColumnType("text");
            entity.Property(m => m.Source).HasMaxLength(200);
            // Unique per user+key — upsert replaces the previous fact for the same key
            entity.HasIndex(m => new { m.UserId, m.Key }).IsUnique();
            entity.HasIndex(m => new { m.UserId, m.Importance });
        });

        modelBuilder.Entity<AgentProvider>(entity =>
        {
            entity.Property(agentProvider => agentProvider.Status).HasMaxLength(50);
            entity.Property(agentProvider => agentProvider.ModelOverride).HasMaxLength(200);
            entity.HasIndex(agentProvider => new { agentProvider.AgentId, agentProvider.ProviderId }).IsUnique();
            entity.HasOne(agentProvider => agentProvider.Agent)
                .WithMany(agent => agent.AgentProviders)
                .HasForeignKey(agentProvider => agentProvider.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(agentProvider => agentProvider.Provider)
                .WithMany(provider => provider.AgentProviders)
                .HasForeignKey(agentProvider => agentProvider.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentSkill>(entity =>
        {
            entity.Property(agentSkill => agentSkill.Status).HasMaxLength(50);
            entity.HasIndex(agentSkill => new { agentSkill.AgentId, agentSkill.SkillId }).IsUnique();
            entity.HasOne(agentSkill => agentSkill.Agent)
                .WithMany(agent => agent.AgentSkills)
                .HasForeignKey(agentSkill => agentSkill.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(agentSkill => agentSkill.Skill)
                .WithMany(skill => skill.AgentSkills)
                .HasForeignKey(agentSkill => agentSkill.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GovernanceSetting>(entity =>
        {
            entity.ToTable("governanceSettings", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_governanceSettings_scopeType_scopeId",
                    "(\"scopeType\" = 'Global' AND \"scopeId\" IS NULL) OR (\"scopeType\" IN ('Tenant', 'Agent') AND \"scopeId\" IS NOT NULL)");
            });
            entity.Property(setting => setting.Id).HasColumnName("id");
            entity.Property(setting => setting.ScopeType)
                .HasColumnName("scopeType")
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(setting => setting.ScopeId).HasColumnName("scopeId");
            entity.Property(setting => setting.EnableBuiltinPromptInjection).HasColumnName("enableBuiltinPromptInjection");
            entity.Property(setting => setting.EnableCustomPromptInjection).HasColumnName("enableCustomPromptInjection");
            entity.Property(setting => setting.EnableAudit).HasColumnName("enableAudit");
            entity.Property(setting => setting.EnableMetrics).HasColumnName("enableMetrics");
            entity.Property(setting => setting.EnableCircuitBreaker).HasColumnName("enableCircuitBreaker");
            entity.Property(setting => setting.BuiltinDetectorConfig)
                .HasColumnName("builtinDetectorConfig")
                .HasColumnType("jsonb");
            entity.Property(setting => setting.IsActive).HasColumnName("isActive");
            entity.Property(setting => setting.CreatedBy).HasColumnName("createdBy").HasMaxLength(500);
            entity.Property(setting => setting.CreatedOn).HasColumnName("createdOn");
            entity.Property(setting => setting.UpdatedBy).HasColumnName("updatedBy").HasMaxLength(500);
            entity.Property(setting => setting.UpdatedOn).HasColumnName("updatedOn");
            entity.HasIndex(setting => new { setting.ScopeType, setting.ScopeId }).IsUnique();
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
