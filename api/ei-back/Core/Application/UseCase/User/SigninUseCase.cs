using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Infrastructure.Extensions;
using ei_back.Infrastructure.Token;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;


namespace ei_back.Core.Application.UseCase.User
{
    public class SigninUseCase : ISignInUseCase
    {
        private const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";
        private readonly TokenConfiguration _tokenConfiguration;
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IEncryptionService _encryptionService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public SigninUseCase(
            TokenConfiguration tokenConfiguration,
            IUserRepository userRepository,
            ITokenService tokenService,
            IEncryptionService encryptionService,
            IRefreshTokenRepository refreshTokenRepository)
        {
            _tokenConfiguration = tokenConfiguration;
            _userRepository = userRepository;
            _tokenService = tokenService;
            _encryptionService = encryptionService;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public TokenDtoReponse Handler(LoginDtoRequest userDtoRequest)
        {
            var user = _userRepository.FindByUserName(userDtoRequest.UserName).Result;
            if (user == null) return null;

            string storedHash = user.Password;
            bool validPassword;

            if (storedHash.StartsWith("$2"))
            {
                validPassword = _encryptionService.VerifyBcryptHash(userDtoRequest.Password, storedHash);
            }
            else
            {
                var shaHash = _encryptionService.ComputeSha256(userDtoRequest.Password);
                validPassword = shaHash == storedHash;
                if (validPassword)
                {
                    user.Password = _encryptionService.ComputeBcryptHash(userDtoRequest.Password);
                    user.UpdatedAt = DateTimeOffset.UtcNow;
                    _userRepository.RefreshUserInfo(user);
                }
            }

            if (!validPassword) return null;
            if (user.EmailVerifiedAt == null) throw new ei_back.Core.Domain.DomainExceptions.EmailVerificationRequiredException();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim("sub", user.Id.ToString()),
                new Claim("fullName", user.FullName),
                new Claim("email", user.Email),
                new Claim("role", user.Role.GetEnumDescription())
            };

            var accessToken = _tokenService.GenerateAccessToken(claims);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = _encryptionService.ComputeSha256Hash(refreshToken);

            _refreshTokenRepository.Create(new Domain.Entity.RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(_tokenConfiguration.DaysToExpiry),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            DateTimeOffset createDate = DateTimeOffset.UtcNow;
            DateTimeOffset expirationDate = createDate.AddMinutes(_tokenConfiguration.Minutes);

            return new TokenDtoReponse(
                true,
                createDate.ToString(DATE_FORMAT),
                expirationDate.ToString(DATE_FORMAT),
                accessToken,
                refreshToken
            );
        }
    }
}
