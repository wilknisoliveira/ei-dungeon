namespace ei_back.Core.Application.UseCase.User.Interfaces
{
    public interface ILogoutUseCase
    {
        void Handler(string userName, string? refreshToken);
    }
}
