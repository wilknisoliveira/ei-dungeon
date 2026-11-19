using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.GoogleAuth;

public class IssueGoogleChallengeUseCaseTests
{
    private readonly GoogleAuthUseCaseTestContext context = new();

    [Fact]
    public async Task Issue_MissingConfiguration_LeavesLocalAccessIndependent()
    {
        A.CallTo(() => context.Verifier.IsConfigured).Returns(false);
        Func<Task> action = () => context.Issue.Handler(GoogleAuthPurpose.SignIn, null, "browser", CancellationToken.None);
        (await action.Should().ThrowAsync<GoogleAuthenticationException>()).Which.Code.Should().Be("google_unavailable");
        A.CallTo(() => context.Challenges.Add(A<GoogleAuthChallenge>._)).MustNotHaveHappened();
    }
}
