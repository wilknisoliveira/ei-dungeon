namespace ei_back.Core.Application.UseCase.User.Interfaces
{
    public interface IDeleteUserUseCase
    {
        Task Handler(Guid userId, CancellationToken cancellationToken = default);
    }
}
