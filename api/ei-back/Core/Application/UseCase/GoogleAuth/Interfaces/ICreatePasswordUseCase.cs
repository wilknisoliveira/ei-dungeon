using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;

namespace ei_back.Core.Application.UseCase.GoogleAuth.Interfaces;

public interface ICreatePasswordUseCase
{
    Task Handler(Guid userId, CreatePasswordDtoRequest request, string browserSecret, CancellationToken cancellationToken);
}
