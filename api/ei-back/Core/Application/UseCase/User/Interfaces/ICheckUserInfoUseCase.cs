using ei_back.Core.Application.UseCase.User.Dtos;

namespace ei_back.Core.Application.UseCase.User.Interfaces
{
    public interface ICheckUserInfoUseCase
    {
        Task<UserInfoCheckResponseDto> Handler(string? username, string? email);
    }
}
