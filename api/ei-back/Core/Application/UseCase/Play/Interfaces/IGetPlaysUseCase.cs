using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Application.Utils;

namespace ei_back.Core.Application.UseCase.Play.Interfaces
{
    public interface IGetPlaysUseCase
    {
        Task<PagedSearchDto<PlayDtoResponse>> Handler(Guid gameId, string sortDirection, int pageSize, int page, Guid userId, CancellationToken cancellationToken);
    }
}
