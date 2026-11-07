using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Infrastructure.Context.Repository
{
    public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(EIContext context) : base(context) { }

        public async Task<RefreshToken?> FindByTokenHash(string tokenHash, CancellationToken cancellationToken = default)
        {
            return await _context.Set<RefreshToken>()
                .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<List<RefreshToken>> FindAllByUserId(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<RefreshToken>()
                .Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        public void DeleteByUserId(Guid userId)
        {
            var tokens = _context.Set<RefreshToken>()
                .Where(x => x.UserId == userId)
                .ToList();

            _context.Set<RefreshToken>().RemoveRange(tokens);
        }
    }
}
