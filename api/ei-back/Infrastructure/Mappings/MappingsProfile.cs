using AutoMapper;
using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.GameInfo.Dtos;
using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Application.UseCase.Role.Dtos;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Domain.Entity;

namespace ei_back.Infrastructure.Mappings
{
    public class MappingsProfile : Profile
    {
        public MappingsProfile()
        {
            //User
            CreateMap<User, UserDtoResponse>();
            CreateMap<User, UserGetDtoResponse>();
            CreateMap<UserDtoRequest, User>()
                .ForMember(dest => dest.Games, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore());
            CreateMap<User, ApplyRoleDtoResponse>();

            //Game
            CreateMap<GameDtoRequest, Game>()
                .ForMember(dest => dest.OwnerUser, opt => opt.Ignore())
                .ForMember(dest => dest.Plays, opt => opt.Ignore());
            CreateMap<Game, GameDtoResponse>();

            //Play
            CreateMap<Play, PlayDtoResponse>();

            //GameInfo
            CreateMap<GameInfo, GameInfoDto>();
            CreateMap<GameInfoDto, GameInfo>();
        }
    }
}
