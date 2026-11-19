using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Domain.Entity;
using ei_back.Infrastructure.Extensions;
using ei_back.Infrastructure.Token;

namespace ei_back.Core.Application.Service.Auth;

public class ApplicationSessionService(ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository,
    IEncryptionService encryptionService, TokenConfiguration tokenConfiguration) : IApplicationSessionService
{
    public TokenDtoReponse Create(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("email", user.Email),
            new("role", user.Role.GetEnumDescription())
        };
        var now = DateTimeOffset.UtcNow;
        var refreshToken = tokenService.GenerateRefreshToken();
        refreshTokenRepository.Create(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = encryptionService.ComputeSha256Hash(refreshToken),
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = now.AddDays(tokenConfiguration.DaysToExpiry)
        });
        return new TokenDtoReponse(true, now.ToString("yyyy-MM-dd HH:mm:ss"),
            now.AddMinutes(tokenConfiguration.Minutes).ToString("yyyy-MM-dd HH:mm:ss"),
            tokenService.GenerateAccessToken(claims), refreshToken);
    }
}
