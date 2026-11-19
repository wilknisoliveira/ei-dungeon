using System.IdentityModel.Tokens.Jwt;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth;
using ei_back.Core.Application.Service.Encryption;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using ei_back.Infrastructure.Token;
namespace ei_back.Tests.Infrastructure;

public class ApplicationSessionTests
{
    private static TokenConfiguration Configuration() => new()
    {
        Secret = new string('s', 64), Issuer = "test", Audience = "test", Minutes = 15, DaysToExpiry = 7
    };

    [Fact]
    public void Session_EmitsInternalSubjectAndStoresOnlyRefreshHash()
    {
        var refresh = A.Fake<IRefreshTokenRepository>();
        var user = new User("player@example.com", null, UserRole.CommonUser) { Id = Guid.NewGuid() };
        var config = Configuration();
        var tokens = new TokenService(config);
        var encryption = new EncryptionService();
        var session = new ApplicationSessionService(tokens, refresh, encryption, config).Create(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(session.AccessToken);
        jwt.Subject.Should().Be(user.Id.ToString());
        jwt.Claims.Should().NotContain(x => x.Type == "unique_name" || x.Type == "fullName");
        A.CallTo(() => refresh.Create(A<RefreshToken>.That.Matches(x => x.UserId == user.Id && x.TokenHash == encryption.ComputeSha256Hash(session.RefreshToken))))
            .MustHaveHappenedOnceExactly();
        new GetUserIdUseCase().Handler(tokens.GetPrincipalFromExpiredToken(session.AccessToken)).Should().Be(user.Id);
    }

    [Fact]
    public void RefreshTokenValidation_RejectsWrongAudienceAndMalformedTokensAsUnauthorized()
    {
        var expected = new TokenService(Configuration());
        var otherConfig = Configuration();
        otherConfig.Audience = "another-app";
        var foreign = new TokenService(otherConfig).GenerateAccessToken(Array.Empty<System.Security.Claims.Claim>());
        Action foreignToken = () => expected.GetPrincipalFromExpiredToken(foreign);
        foreignToken.Should().Throw<UnauthorizedException>();
        Action malformed = () => expected.GetPrincipalFromExpiredToken("malformed-token");
        malformed.Should().Throw<UnauthorizedException>();
    }
}
