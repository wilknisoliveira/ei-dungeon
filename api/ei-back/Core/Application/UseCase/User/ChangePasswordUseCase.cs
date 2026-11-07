using AutoMapper;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;

namespace ei_back.Core.Application.UseCase.User
{
    public class ChangePasswordUseCase(
        IUserRepository userRepository,
        IEncryptionService encryptionService,
        IMapper mapper) : IChangePasswordUseCase
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly IMapper _mapper = mapper;

        public async Task<UserGetDtoResponse> Handler(string userName, PasswordDtoRequest passwordDtoRequest)
        {
            var user = await _userRepository.FindByUserName(userName) ??
                throw new NotFoundException($"User '{userName}' not found.");

            string storedHash = user.Password;
            bool validPassword;

            if (storedHash.StartsWith("$2"))
            {
                validPassword = _encryptionService.VerifyBcryptHash(passwordDtoRequest.CurrentPassword, storedHash);
            }
            else
            {
                validPassword = _encryptionService.ComputeSha256(passwordDtoRequest.CurrentPassword) == storedHash;
            }

            if (!validPassword)
                throw new BadRequestException("Wrong password!");

            user.Password = _encryptionService.ComputeBcryptHash(passwordDtoRequest.NewPassword);
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var response = _userRepository.Update(user);

            return _mapper.Map<UserGetDtoResponse>(response);
        }
    }
}
