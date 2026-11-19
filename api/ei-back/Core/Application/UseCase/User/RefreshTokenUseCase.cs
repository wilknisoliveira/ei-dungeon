using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using ei_back.Infrastructure.Token;

namespace ei_back.Core.Application.UseCase.User;

public class RefreshTokenUseCase(ITokenService tokenService, IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository, IEncryptionService encryptionService,
    IGetUserIdUseCase getUserIdUseCase, IApplicationSessionService sessionService) : IRefreshTokenUseCase
{
    public async Task<TokenDtoReponse> Handler(RefreshTokenDtoRequest request, CancellationToken cancellationToken = default)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        var userId = getUserIdUseCase.Handler(principal);
        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user?.EmailVerifiedAt == null) throw new UnauthorizedException("Invalid credentials.");
        var hash = encryptionService.ComputeSha256Hash(request.RefreshToken);
        var stored = await refreshTokenRepository.FindByTokenHash(hash, cancellationToken);
        if (stored == null || stored.UserId != userId || stored.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new UnauthorizedException("Invalid credentials.");
        refreshTokenRepository.Delete(stored.Id);
        return sessionService.Create(user);
    }
}
