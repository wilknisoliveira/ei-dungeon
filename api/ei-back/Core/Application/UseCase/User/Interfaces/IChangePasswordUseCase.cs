using ei_back.Core.Application.UseCase.User.Dtos;

namespace ei_back.Core.Application.UseCase.User.Interfaces
{
    public interface IChangePasswordUseCase
    {
        Task<UserGetDtoResponse> Handler(string userName, PasswordDtoRequest passwordDtoRequest);
    }
}
