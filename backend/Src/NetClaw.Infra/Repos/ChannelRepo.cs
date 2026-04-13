using NetClaw.Domains.Entities;
using NetClaw.Domains.Repos;
using NetClaw.EfCore.Extensions.Repos;
using NetClaw.Infra.Contexts;

namespace NetClaw.Infra.Repos;

internal sealed class ChannelRepo(AppDbContext dbContext)
    : Repository<Channel>(dbContext), IChannelRepo
{
}
