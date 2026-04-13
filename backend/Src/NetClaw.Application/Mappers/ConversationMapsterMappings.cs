using Mapster;
using NetClaw.Contracts;
using NetClaw.Domains.Entities;

namespace NetClaw.Application.Mappers;

public class ConversationMapsterMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ConversationMessage, ConversationMessageResponse>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.CreatedAt, src => src.CreatedOn.ToString("O"));

        config.NewConfig<Conversation, ConversationListItemResponse>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.TargetId, src => src.TargetId != null ? src.TargetId.Value.ToString() : null)
            .Map(dest => dest.LastMessageAt, src => src.LastMessageOn.ToString("O"))
            .Map(dest => dest.CreatedAt, src => src.CreatedOn.ToString("O"));

        config.NewConfig<Conversation, ConversationResponse>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.TargetId, src => src.TargetId != null ? src.TargetId.Value.ToString() : null)
            .Map(dest => dest.LastMessageAt, src => src.LastMessageOn.ToString("O"))
            .Map(dest => dest.CreatedAt, src => src.CreatedOn.ToString("O"))
            .Map(dest => dest.Messages, src => src.Messages
                .OrderBy(item => item.Sequence)
                .ThenBy(item => item.CreatedOn)
                .ToList());
    }
}
