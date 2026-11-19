using ei_back.Core.Domain.Entity;
namespace ei_back.Core.Application.Repository;

public interface IExternalIdentityRepository
{
    Task<ExternalIdentity?> FindBySubjectAsync(string subject, CancellationToken cancellationToken = default);
    Task<ExternalIdentity?> FindByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(ExternalIdentity identity);
}
