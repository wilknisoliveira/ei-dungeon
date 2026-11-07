using ei_back.Core.Application.UseCase.GameInfo.Dtos;

namespace ei_back.Core.Application.UseCase.GameInfo.Interfaces
{
    public interface ICreateGameInfoUseCase
    {
        Task<IEnumerable<GameInfoDto>> Handler(IEnumerable<GameInfoDto> gameInfoDtos, CancellationToken cancellationToken);
    }
}
