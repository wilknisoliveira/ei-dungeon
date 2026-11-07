using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Infrastructure.Context.Repository
{
    public class GameInfoRepository : GenericRepository<GameInfo>, IGameInfoRepository
    {
        public GameInfoRepository(EIContext context) : base(context)
        {
        }

        public async Task<IEnumerable<GameInfo>?> GetItemsByValuesAndType(List<string> values, InfoType type, CancellationToken cancellationToken)
        {
            return await _context.GameInfos.Where(x => x.Type.Equals(type) && values.Contains(x.Value)).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<GameInfo>> GetLimitRandomValuesByType(int limit, InfoType type, CancellationToken cancellationToken)
        {
            return await _context.GameInfos
                .FromSqlRaw("SELECT * FROM game_infos WHERE Type = {0} ORDER BY RANDOM() LIMIT {1}", type, limit)
                .ToListAsync(cancellationToken);
        }
    }
}
