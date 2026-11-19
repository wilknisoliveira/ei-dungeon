using ei_back.Core.Domain.Entity;
namespace ei_back.Core.Application.Repository;

public interface IGoogleAuthChallengeRepository
{
    void Add(GoogleAuthChallenge challenge);
    Task<GoogleAuthChallenge?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ConsumeAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task PruneAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}
