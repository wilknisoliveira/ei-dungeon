using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.Utils;

namespace ei_back.Core.Application.UseCase.Game.Interfaces
{
    public interface IGetGamesUseCase
    {
        Task<PagedSearchDto<GameDtoResponse>> Handler(string sortDirection, int pageSize, int page, string userName, CancellationToken cancellationToken);
    }
}
