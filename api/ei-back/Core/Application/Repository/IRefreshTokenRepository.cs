using ei_back.Core.Domain.Entity;

namespace ei_back.Core.Application.Repository
{
    public interface IRefreshTokenRepository : IRepository<RefreshToken>
    {
        Task<RefreshToken?> FindByTokenHash(string tokenHash, CancellationToken cancellationToken = default);
        Task<List<RefreshToken>> FindAllByUserId(Guid userId, CancellationToken cancellationToken = default);
        void DeleteByUserId(Guid userId);
    }
}
