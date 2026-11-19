using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.UseCase.GoogleAuth.Interfaces;

public interface IIssueGoogleChallengeUseCase
{
    Task<GoogleChallengeDtoResponse> Handler(GoogleAuthPurpose purpose, Guid? userId, string browserSecret, CancellationToken cancellationToken);
}
