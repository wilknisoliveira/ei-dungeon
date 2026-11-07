using ei_back.Core.Application.UseCase.Play.Dtos;

namespace ei_back.Core.Application.Service.Play.Interfaces;

public interface IPlayAnalyzerService
{
    Task<AnalyzerDtoResponse> Handler(
        List<Domain.Entity.Play> plays, 
        Domain.Entity.Game game, 
        CancellationToken cancellationToken);
}