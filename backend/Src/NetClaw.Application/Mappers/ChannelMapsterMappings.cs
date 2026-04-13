using Mapster;
using NetClaw.Contracts;
using NetClaw.Domains.Entities;

namespace NetClaw.Application.Mappers;

public sealed class ChannelMapsterMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Channel, ChannelResponse>()
            .Map(dest => dest.HasCredentials, src => !string.IsNullOrWhiteSpace(src.EncryptedCredentials));
    }
}
