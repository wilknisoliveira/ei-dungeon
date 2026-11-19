using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;

namespace ei_back.Core.Application.UseCase.GoogleAuth.Interfaces;

public interface ILinkGoogleUseCase
{
    Task Handler(Guid userId, GoogleAuthRequestDto request, string browserSecret, CancellationToken cancellationToken);
}
