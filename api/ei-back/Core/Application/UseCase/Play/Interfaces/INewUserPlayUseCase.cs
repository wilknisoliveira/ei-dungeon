using System.Runtime.CompilerServices;
using ei_back.Core.Application.UseCase.Play.Dtos;

namespace ei_back.Core.Application.UseCase.Play.Interfaces
{
    public interface INewUserPlayUseCase
    {
        IAsyncEnumerable<StreamPlayDtoResponse> Handler(PlayDtoRequest playDtoRequest, Guid userId, CancellationToken cancellationToken);
    }
}
