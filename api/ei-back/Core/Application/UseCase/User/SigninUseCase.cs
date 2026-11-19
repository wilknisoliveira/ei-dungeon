using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.DomainExceptions;

namespace ei_back.Core.Application.UseCase.User;

public class SigninUseCase(IUserRepository userRepository, IEncryptionService encryptionService,
    IApplicationSessionService sessionService) : ISignInUseCase
{
    public async Task<TokenDtoReponse?> Handler(LoginDtoRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByEmail(request.Email, cancellationToken);
        if (string.IsNullOrEmpty(user?.Password)) return null;
        var storedHash = user.Password;
        var bcrypt = storedHash.StartsWith("$2", StringComparison.Ordinal);
        var valid = bcrypt
            ? encryptionService.VerifyBcryptHash(request.Password, storedHash)
            : encryptionService.ComputeSha256(request.Password) == storedHash;
        if (!valid) return null;
        if (user.EmailVerifiedAt == null) throw new EmailVerificationRequiredException();
        if (!bcrypt)
        {
            user.Password = encryptionService.ComputeBcryptHash(request.Password);
            user.UpdatedAt = DateTimeOffset.UtcNow;
        }
        return sessionService.Create(user);
    }
}
