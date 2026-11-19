using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Application.UseCase.User.Dtos;

namespace ei_back.Core.Application.UseCase.GoogleAuth.Interfaces;

public interface IGoogleSigninUseCase
{
    Task<TokenDtoReponse> Handler(GoogleAuthRequestDto request, string browserSecret, CancellationToken cancellationToken);
}
