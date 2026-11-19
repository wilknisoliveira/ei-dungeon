using System.Security.Claims;
using ei_back.Core.Application.UseCase.User;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
namespace ei_back.Tests.Core.Application.UseCase.User;

public class GetUserIdUseCaseTests
{
    [Theory]
    [InlineData("sub")]
    [InlineData(ClaimTypes.NameIdentifier)]
    public void Handler_InternalSubject_ReturnsId(string claimType)
    {
        var id = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(claimType, id.ToString()), new Claim(ClaimTypes.Name, "legacy-name") }, "test"));
        new GetUserIdUseCase().Handler(principal).Should().Be(id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("legacy-name")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Handler_InvalidSubject_Rejects(string subject)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", subject) }));
        Action action = () => new GetUserIdUseCase().Handler(principal);
        action.Should().Throw<UnauthorizedException>();
    }
}
