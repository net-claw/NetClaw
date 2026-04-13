using Microsoft.EntityFrameworkCore;
using Mapster;
using MapsterMapper;
using NetClaw.Contracts;
using NetClaw.Contracts.Channel;
using NetClaw.Domains.Repos;

namespace NetClaw.Application.Features.Channel.Queries;

public sealed class GetChannelByIdHandler
{
    public async Task<ChannelResponse?> Handle(
        GetChannelById query,
        IChannelRepo repo,
        IMapper mapper,
        CancellationToken ct)
    {
        var channel = await repo.Query()
            .AsNoTracking()
            .Where(item => item.Id == query.ChannelId && item.DeletedAt == null)
            .ProjectToType<ChannelResponse>(mapper.Config)
            .FirstOrDefaultAsync(ct);

        return channel;
    }
}
