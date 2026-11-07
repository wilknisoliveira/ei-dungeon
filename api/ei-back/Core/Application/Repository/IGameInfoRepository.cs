using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.Repository
{
    public interface IGameInfoRepository : IRepository<GameInfo>
    {
        Task<IEnumerable<GameInfo>?> GetItemsByValuesAndType(List<string> values, InfoType type, CancellationToken cancellationToken);
        Task<IEnumerable<GameInfo>> GetLimitRandomValuesByType(int limit, InfoType type, CancellationToken cancellationToken);
    }
}
