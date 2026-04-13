using Mapster;
using NetClaw.Contracts;
using NetClaw.Domains.Entities.Identity;

namespace NetClaw.Application.Mappers;

public class IdentityMapsterMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AppUser, UserResponse>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.Email, src => src.Email ?? string.Empty)
            .Map(dest => dest.Phone, src => src.PhoneNumber ?? string.Empty)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToString("O"));

        config.NewConfig<AppRole, RoleResponse>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToString("O"))
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt.ToString("O"));
    }
}
