using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
namespace ei_back.Infrastructure.Context.Repository;

public class GoogleAuthChallengeRepository(EIContext context) : IGoogleAuthChallengeRepository
{
    public void Add(GoogleAuthChallenge challenge) => context.GoogleAuthChallenges.Add(challenge);
    public Task<GoogleAuthChallenge?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.GoogleAuthChallenges.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<bool> ConsumeAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Challenge consumption requires a transaction.");
        return await context.GoogleAuthChallenges.Where(x => x.Id == id && x.ConsumedAt == null && x.ExpiresAt > now)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.ConsumedAt, now), cancellationToken) == 1;
    }
    public Task PruneAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        context.GoogleAuthChallenges.Where(x => x.ExpiresAt <= now).ExecuteDeleteAsync(cancellationToken);
}
