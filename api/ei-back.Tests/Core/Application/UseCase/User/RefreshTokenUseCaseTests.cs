using UserEntity = ei_back.Core.Domain.Entity.User;
using System.Security.Claims;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using ei_back.Infrastructure.Token;
namespace ei_back.Tests.Core.Application.UseCase.User;

public class RefreshTokenUseCaseTests
{
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task Handler_InternalSubject_RefreshesOnlyEligibleOwner(bool verified, bool expired, bool otherOwner)
    {
        var users = A.Fake<IUserRepository>();
        var tokens = A.Fake<ITokenService>();
        var refresh = A.Fake<IRefreshTokenRepository>();
        var encryption = A.Fake<IEncryptionService>();
        var sessions = A.Fake<IApplicationSessionService>();
        var user = new UserEntity("player@example.com", null, UserRole.CommonUser) { Id = Guid.NewGuid(), EmailVerifiedAt = verified ? DateTimeOffset.UtcNow : null };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", user.Id.ToString()), new Claim(ClaimTypes.Name, "obsolete-username") }));
        A.CallTo(() => tokens.GetPrincipalFromExpiredToken("old-access")).Returns(principal);
        A.CallTo(() => users.FindByIdAsync(user.Id, CancellationToken.None)).Returns(user);
        A.CallTo(() => encryption.ComputeSha256Hash("old-refresh")).Returns("hash");
        var stored = new RefreshToken { Id = Guid.NewGuid(), UserId = otherOwner ? Guid.NewGuid() : user.Id, ExpiresAt = DateTimeOffset.UtcNow.AddDays(expired ? -1 : 1) };
        A.CallTo(() => refresh.FindByTokenHash("hash", CancellationToken.None)).Returns(stored);
        var result = new TokenDtoReponse(true, "created", "expires", "new-access", "new-refresh");
        A.CallTo(() => sessions.Create(user)).Returns(result);
        var useCase = new RefreshTokenUseCase(tokens, users, refresh, encryption, new GetUserIdUseCase(), sessions);
        if (verified && !expired && !otherOwner)
        {
            (await useCase.Handler(new() { AccessToken = "old-access", RefreshToken = "old-refresh" })).Should().BeSameAs(result);
            A.CallTo(() => refresh.Delete(stored.Id)).MustHaveHappenedOnceExactly();
        }
        else
        {
            Func<Task> action = () => useCase.Handler(new() { AccessToken = "old-access", RefreshToken = "old-refresh" });
            await action.Should().ThrowAsync<UnauthorizedException>();
            A.CallTo(() => sessions.Create(A<UserEntity>._)).MustNotHaveHappened();
        }
    }
}
