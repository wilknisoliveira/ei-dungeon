using System.Security.Cryptography;
using System.Text;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Application.UseCase.GoogleAuth.Interfaces;

namespace ei_back.Core.Application.UseCase.GoogleAuth;

public class IssueGoogleChallengeUseCase(IGoogleIdentityVerifier identityVerifier, IGoogleAuthFlowService authFlowService,
    IGoogleAuthChallengeRepository challengeRepository, IUnitOfWork unitOfWork) : IIssueGoogleChallengeUseCase
{
    public async Task<GoogleChallengeDtoResponse> Handler(GoogleAuthPurpose purpose, Guid? userId,
        string browserSecret, CancellationToken cancellationToken)
    {
        if (!identityVerifier.IsConfigured) throw Error("google_unavailable");
        if (purpose != GoogleAuthPurpose.SignIn)
            await authFlowService.GetVerifiedUserAsync(userId ?? throw Error("google_invalid_credential"), cancellationToken);
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var now = DateTimeOffset.UtcNow;
        var challenge = new GoogleAuthChallenge
        {
            Id = Guid.NewGuid(), Purpose = purpose,
            UserId = purpose == GoogleAuthPurpose.SignIn ? null : userId,
            NonceHash = Hash(nonce), BrowserSecretHash = Hash(browserSecret),
            ExpiresAt = now.AddMinutes(5), CreatedAt = now, UpdatedAt = now
        };
        await challengeRepository.PruneAsync(now, cancellationToken);
        challengeRepository.Add(challenge);
        await unitOfWork.CommitAsync(cancellationToken);
        return new GoogleChallengeDtoResponse(challenge.Id, nonce, challenge.ExpiresAt);
    }

    private static GoogleAuthenticationException Error(string code) => new(code);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
