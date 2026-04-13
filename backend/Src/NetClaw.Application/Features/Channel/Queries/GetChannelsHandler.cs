using Microsoft.EntityFrameworkCore;
using Mapster;
using MapsterMapper;
using NetClaw.Contracts;
using NetClaw.Domains.Repos;

namespace NetClaw.Application.Features.Channel.Queries;

public sealed class GetChannelsHandler
{
    public async Task<ChannelPageResponse> Handle(
        Contracts.Channel.GetChannels query,
        IChannelRepo repo,
        IMapper mapper,
        CancellationToken ct)
    {
        var searchText = query.SearchText?.Trim();
        var kind = query.Kind?.Trim().ToLowerInvariant();
        var status = query.Status?.Trim().ToLowerInvariant();

        var channels = repo.Query()
            .AsNoTracking()
            .Where(channel => channel.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            channels = channels.Where(channel =>
                EF.Functions.Like(channel.Name, $"%{searchText}%") ||
                EF.Functions.Like(channel.Kind, $"%{searchText}%") ||
                EF.Functions.Like(channel.Status, $"%{searchText}%"));
        }

        if (!string.IsNullOrWhiteSpace(kind))
        {
            channels = channels.Where(channel => channel.Kind == kind);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            channels = channels.Where(channel => channel.Status == status);
        }

        channels = (query.OrderBy ?? string.Empty).ToLowerInvariant() switch
        {
            "kind" => query.Ascending
                ? channels.OrderBy(channel => channel.Kind).ThenBy(channel => channel.Name)
                : channels.OrderByDescending(channel => channel.Kind).ThenByDescending(channel => channel.Name),
            "status" => query.Ascending
                ? channels.OrderBy(channel => channel.Status).ThenBy(channel => channel.Name)
                : channels.OrderByDescending(channel => channel.Status).ThenByDescending(channel => channel.Name),
            "updatedat" => query.Ascending
                ? channels.OrderBy(channel => channel.UpdatedOn ?? channel.CreatedOn).ThenBy(channel => channel.Name)
                : channels.OrderByDescending(channel => channel.UpdatedOn ?? channel.CreatedOn).ThenByDescending(channel => channel.Name),
            _ => query.Ascending
                ? channels.OrderBy(channel => channel.Name)
                : channels.OrderByDescending(channel => channel.Name),
        };

        var totalItems = await channels.CountAsync(ct);
        var items = await channels
            .ProjectToType<ChannelResponse>(mapper.Config)
            .Skip(query.PageIndex * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new ChannelPageResponse(
            items,
            query.PageIndex,
            query.PageSize,
            totalItems,
            totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)query.PageSize));
    }
}
