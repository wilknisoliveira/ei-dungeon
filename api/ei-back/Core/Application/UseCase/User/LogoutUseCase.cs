using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;

namespace ei_back.Core.Application.UseCase.User
{
    public class LogoutUseCase : ILogoutUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IEncryptionService _encryptionService;

        public LogoutUseCase(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IEncryptionService encryptionService)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _encryptionService = encryptionService;
        }

        public async Task Handler(Guid userId, string? refreshToken, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
            if (user == null)
                throw new NotFoundException($"User '{userId}' not found.");

            if (!string.IsNullOrEmpty(refreshToken))
            {
                var tokenHash = _encryptionService.ComputeSha256Hash(refreshToken);
                var storedToken = await _refreshTokenRepository.FindByTokenHash(tokenHash, cancellationToken);
                if (storedToken != null && storedToken.UserId == user.Id)
                    _refreshTokenRepository.Delete(storedToken.Id);
            }
            else
            {
                _refreshTokenRepository.DeleteByUserId(user.Id);
            }
        }
    }
}
