using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.Repository
{
    public interface IPlayRepository : IRepository<Play>
    {
        Task<List<Play>> GetPlaysByGameAndSizeButSummaryPlay(Guid gameId, int size, int offset, string sort, CancellationToken cancellationToken);
        Task<Play?> GetLastPlayByPlayTypeAndGameId(Guid gameId, PlayType playType, CancellationToken cancellationToken);
        Task<int> CountPlaysByGameButSummaryPlay(Guid gameId, CancellationToken cancellationToken);
        Task<List<Play>> GetPlayWhereCreatedAtIsUpperThan(Guid gameId, DateTimeOffset createdAt, CancellationToken cancellationToken);
        Task<List<Play>> GetAllByGameId(Guid gameId, CancellationToken cancellationToken);
        Task<List<Play>> GetLastNBeforeDate(Guid gameId, int limit, DateTimeOffset limitDate, CancellationToken cancellationToken);
    }
}
