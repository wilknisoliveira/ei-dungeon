using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Infrastructure.Context.Repository
{
    public class GameRepository : GenericRepository<Game>, IGameRepository
    {
        public GameRepository(EIContext context) : base(context)
        {
        }

        public async Task<bool> CheckIfExistGameByUser(Guid gameId, Guid OwnerUserId, CancellationToken cancellationToken)
        {
            return await _context.Games.AnyAsync(x => x.Id.Equals(gameId) && x.OwnerUserId.Equals(OwnerUserId), cancellationToken);
        }

        public async Task<Game?> GetGameByIdAndOwnerUserName(Guid id, string userName, CancellationToken cancellationToken)
        {
            return await _context.Games.Include(x => x.OwnerUser).FirstOrDefaultAsync(x => x.Id.Equals(id) && x.OwnerUser.UserName.Equals(userName), cancellationToken: cancellationToken);
        }

        public async Task<List<Game>> FindWithPagedSearchAsync(string sort, int size, int offset, Guid ownerUserId, CancellationToken cancellationToken = default)
        {
            IQueryable<Game> query = _context.Games.Where(x => x.OwnerUserId.Equals(ownerUserId));

            query = sort == "desc"
                ? query.OrderByDescending(x => x.LastPlayedAt ?? x.UpdatedAt)
                : query.OrderBy(x => x.LastPlayedAt ?? x.UpdatedAt);

            return await query.Skip(offset).Take(size).ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        {
            return await _context.Games.Where(x => x.OwnerUserId.Equals(ownerUserId)).CountAsync(cancellationToken);
        }
    }
}
