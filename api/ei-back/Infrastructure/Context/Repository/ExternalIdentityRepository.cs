using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
namespace ei_back.Infrastructure.Context.Repository;

public class ExternalIdentityRepository(EIContext context) : IExternalIdentityRepository
{
    public Task<ExternalIdentity?> FindBySubjectAsync(string subject, CancellationToken cancellationToken = default) =>
        context.ExternalIdentities.SingleOrDefaultAsync(x => x.Provider == "Google" && x.Subject == subject, cancellationToken);
    public Task<ExternalIdentity?> FindByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.ExternalIdentities.SingleOrDefaultAsync(x => x.Provider == "Google" && x.UserId == userId, cancellationToken);
    public void Add(ExternalIdentity identity) => context.ExternalIdentities.Add(identity);
}
