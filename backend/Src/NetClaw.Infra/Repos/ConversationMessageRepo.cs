using NetClaw.Domains.Entities;
using NetClaw.Domains.Repos;
using NetClaw.EfCore.Extensions.Repos;
using NetClaw.Infra.Contexts;

namespace NetClaw.Infra.Repos;

internal sealed class ConversationMessageRepo(AppDbContext dbContext)
    : Repository<ConversationMessage>(dbContext), IConversationMessageRepo
{
}
