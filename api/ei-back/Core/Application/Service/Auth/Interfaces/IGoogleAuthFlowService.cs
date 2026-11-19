using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.Service.Auth.Interfaces;

public interface IGoogleAuthFlowService
{
    /// <summary>Validates browser, nonce, user and purpose, then atomically consumes the proof inside the caller's transaction.</summary>
    Task ConsumeAsync(Guid challengeId, string browserSecret, GoogleIdentity identity,
        GoogleAuthPurpose purpose, Guid? userId, CancellationToken cancellationToken);
    Task<User> GetVerifiedUserAsync(Guid userId, CancellationToken cancellationToken);
    void AddLink(Guid userId, string subject);
    /// <summary>Commits the complete use-case operation atomically and maps persistence conflicts to a Google outcome.</summary>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
