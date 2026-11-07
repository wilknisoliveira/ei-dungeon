using ei_back.Core.Domain.Entity;

namespace ei_back.Core.Application.Service.Play.Interfaces
{
    public interface IGeneratePlaysSummaryService
    {
        Task Handler(Guid gameId, List<Domain.Entity.Play> plays, CancellationToken cancellationToken);
    }
}
