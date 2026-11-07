using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Infrastructure.Context.Repository
{
    public class PlayRepository : GenericRepository<Play>, IPlayRepository
    {
        public PlayRepository(EIContext context) : base(context)
        {
        }

        public Task<List<Play>> GetPlaysByGameAndSizeButSummaryPlay(Guid gameId, int size, int offset, string sort, CancellationToken cancellationToken)
        {
            IQueryable<Play> query = _context.Plays
                .Where(x => x.GameId.Equals(gameId) && !x.PlayType.Equals(PlayType.Summary));

            query = sort == "desc"
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt);

            return query.Skip(offset).Take(size).ToListAsync(cancellationToken);
        }

        public Task<Play?> GetLastPlayByPlayTypeAndGameId(Guid gameId, PlayType playType, CancellationToken cancellationToken)
        {
            return _context.Plays
                .Where(x => x.GameId.Equals(gameId) && x.PlayType.Equals(playType))
                .OrderByDescending(x => x.UpdatedAt)
               .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<int> CountPlaysByGameButSummaryPlay(Guid gameId, CancellationToken cancellationToken)
        {
            return _context.Plays
                .Where(x => x.GameId.Equals(gameId) && !x.PlayType.Equals(PlayType.Summary))
                .CountAsync(cancellationToken);
        }

        public Task<List<Play>> GetPlayWhereCreatedAtIsUpperThan(Guid gameId, DateTimeOffset createdAt, CancellationToken cancellationToken)
        {
            return _context.Plays
                .Where(x => x.GameId.Equals(gameId) && x.CreatedAt > createdAt)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public Task<List<Play>> GetAllByGameId(Guid gameId, CancellationToken cancellationToken)
        {
            return _context.Plays
                .Where(x => x.GameId.Equals(gameId))
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public Task<List<Play>> GetLastNBeforeDate(Guid gameId, int limit, DateTimeOffset limitDate, CancellationToken cancellationToken)
        {
            return _context.Plays
                .Where(x => x.GameId.Equals(gameId) && x.CreatedAt < limitDate)
                .OrderByDescending(x => x.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
    }
}
