using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;

namespace ei_back.Core.Application.UseCase.GoogleAuth.Interfaces;

public interface IGetCurrentAccountUseCase
{
    Task<CurrentAccountDtoResponse> Handler(Guid userId, CancellationToken cancellationToken);
}
