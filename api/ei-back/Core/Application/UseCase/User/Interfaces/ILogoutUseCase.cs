namespace ei_back.Core.Application.UseCase.User.Interfaces
{
    public interface ILogoutUseCase
    {
        Task Handler(Guid userId, string? refreshToken, CancellationToken cancellationToken = default);
    }
}
