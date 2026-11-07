namespace ei_back.Core.Application.UseCase.Game.Interfaces;

public interface IDeleteGameUseCase
{
    Task Handler(Guid gameId, string userName, CancellationToken cancellationToken);
}