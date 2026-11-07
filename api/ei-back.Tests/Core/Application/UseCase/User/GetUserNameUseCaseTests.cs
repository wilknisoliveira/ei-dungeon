using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ei_back.Tests.Core.Application.UseCase.User
{
    public class GetUserNameUseCaseTests
    {
        [Fact]
        public void Handler_WhenUserHasName_ReturnsUserName()
        {
            var logger = A.Fake<ILogger<GetUserNameUseCase>>();
            var useCase = new GetUserNameUseCase(logger);
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }));

            var result = useCase.Handler(principal);

            result.Should().Be("testuser");
        }

        [Fact]
        public void Handler_WhenUserHasNoName_ThrowsInternalServerErrorException()
        {
            var logger = A.Fake<ILogger<GetUserNameUseCase>>();
            var useCase = new GetUserNameUseCase(logger);
            var principal = new ClaimsPrincipal(new ClaimsIdentity());

            Action act = () => useCase.Handler(principal);

            act.Should().Throw<InternalServerErrorException>()
                .WithMessage("Something went wrong while attempting to get the user logged credential.");
        }
    }
}
