namespace ei_back.Core.Application.UseCase.GoogleAuth.Dtos;

public record GoogleChallengeDtoResponse(Guid ChallengeId, string Nonce, DateTimeOffset ExpiresAt);
