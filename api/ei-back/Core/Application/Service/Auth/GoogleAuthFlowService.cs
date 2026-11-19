using System.Security.Cryptography;
using System.Text;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Core.Application.Service.Auth;

public class GoogleAuthFlowService(IUserRepository userRepository,
    IExternalIdentityRepository externalIdentityRepository, IGoogleAuthChallengeRepository challengeRepository,
    IUnitOfWork unitOfWork) : IGoogleAuthFlowService
{
    public async Task ConsumeAsync(Guid challengeId, string browserSecret, GoogleIdentity identity,
        GoogleAuthPurpose purpose, Guid? userId, CancellationToken cancellationToken)
    {
        var attempt = await challengeRepository.FindAsync(challengeId, cancellationToken);
        if (attempt == null || attempt.Purpose != purpose || attempt.UserId != userId ||
            string.IsNullOrEmpty(browserSecret) || string.IsNullOrEmpty(identity.Nonce) ||
            !Matches(attempt.BrowserSecretHash, browserSecret) || !Matches(attempt.NonceHash, identity.Nonce) ||
            !await challengeRepository.ConsumeAsync(challengeId, DateTimeOffset.UtcNow, cancellationToken))
            throw Error("google_invalid_credential");
    }

    public async Task<UserEntity> GetVerifiedUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user?.EmailVerifiedAt == null) throw Error("google_invalid_credential");
        return user;
    }

    public void AddLink(Guid userId, string subject)
    {
        var now = DateTimeOffset.UtcNow;
        externalIdentityRepository.Add(new ExternalIdentity
        {
            Id = Guid.NewGuid(), UserId = userId, Subject = subject, CreatedAt = now, UpdatedAt = now
        });
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(operation, cancellationToken);
        }
        catch (PersistenceConflictException)
        {
            throw Error("google_link_conflict");
        }
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static bool Matches(string hash, string value) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(Hash(value)));
    private static GoogleAuthenticationException Error(string code) => new(code);
}
