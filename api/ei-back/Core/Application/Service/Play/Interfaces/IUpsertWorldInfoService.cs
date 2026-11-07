using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.Service.Play.Interfaces;

public interface IUpsertWorldInfoService
{
    Task<string> Handler(string playerInfo, GameLanguage language, CancellationToken cancellationToken);
    Task Handler(Guid gameId, List<Domain.Entity.Play> plays, CancellationToken cancellationToken);
}