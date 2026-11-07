using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;

namespace ei_back.Core.Application.UseCase.User
{
    public class CheckUserInfoUseCase(
        IUserRepository userRepository) : ICheckUserInfoUseCase
    {
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<UserInfoCheckResponseDto> Handler(string? username, string? email)
        {
            Domain.Entity.User? userByUsername = null;
            Domain.Entity.User? userByEmail = null;

            if (!string.IsNullOrWhiteSpace(username))
                userByUsername = await _userRepository.FindByUserName(username);

            if (!string.IsNullOrWhiteSpace(email))
                userByEmail = await _userRepository.FindByEmail(email);

            return new UserInfoCheckResponseDto
            {
                UsernameAvailable = userByUsername == null,
                EmailAvailable = userByEmail == null
            };
        }
    }
}
