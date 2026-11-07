using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using ei_back.Infrastructure.Extensions;
using ei_back.Infrastructure.Token;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace ei_back.Core.Application.UseCase.User
{
    public class RefreshTokenUseCase : IRefreshTokenUseCase
    {
        private const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly TokenConfiguration _tokenConfiguration;
        private readonly IEncryptionService _encryptionService;

        public RefreshTokenUseCase(
            ITokenService tokenService,
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            TokenConfiguration tokenConfiguration,
            IEncryptionService encryptionService)
        {
            _tokenService = tokenService;
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenConfiguration = tokenConfiguration;
            _encryptionService = encryptionService;
        }

        public TokenDtoReponse Handler(RefreshTokenDtoRequest request)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            var userName = principal.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
                throw new UnauthorizedException("Invalid credentials.");

            var user = _userRepository.FindByUserName(userName).Result;
            if (user == null)
                throw new UnauthorizedException("Invalid credentials.");

            if (user.EmailVerifiedAt == null) throw new UnauthorizedException("Invalid credentials.");

            var refreshTokenHash = _encryptionService.ComputeSha256Hash(request.RefreshToken);
            var storedToken = _refreshTokenRepository.FindByTokenHash(refreshTokenHash).Result;
            if (storedToken == null || storedToken.UserId != user.Id || storedToken.ExpiresAt < DateTimeOffset.UtcNow)
                throw new UnauthorizedException("Invalid credentials.");

            _refreshTokenRepository.Delete(storedToken.Id);

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
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var newRefreshTokenHash = _encryptionService.ComputeSha256Hash(newRefreshToken);

            _refreshTokenRepository.Create(new Domain.Entity.RefreshToken
            {
                UserId = user.Id,
                TokenHash = newRefreshTokenHash,
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
                newRefreshToken
            );
        }
    }
}
