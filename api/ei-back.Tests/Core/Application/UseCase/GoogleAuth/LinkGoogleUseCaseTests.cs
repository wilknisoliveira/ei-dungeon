using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.GoogleAuth;

public class LinkGoogleUseCaseTests
{
    private readonly GoogleAuthUseCaseTestContext context = new();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Link_OnlyMatchingEmailAndUnclaimedSubjectCanBeLinked(bool wrongOwner)
    {
        context.ConfigureAttempt(GoogleAuthPurpose.Link, context.User.Id);
        if (wrongOwner) A.CallTo(() => context.Identities.FindBySubjectAsync("subject", A<CancellationToken>._)).Returns(new ExternalIdentity { UserId = Guid.NewGuid() });
        else context.SetIdentity("another@gmail.com", true, null);
        Func<Task> action = () => context.Link.Handler(context.User.Id, context.Request, "browser", CancellationToken.None);
        await action.Should().ThrowAsync<GoogleAuthenticationException>();
        A.CallTo(() => context.Identities.Add(A<ExternalIdentity>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Link_IndependentlyVerifiedExternalEmail_CanBeLinked()
    {
        context.User.Email = "player@outlook.com";
        context.User.NormalizedEmail = "PLAYER@OUTLOOK.COM";
        context.ConfigureAttempt(GoogleAuthPurpose.Link, context.User.Id);
        context.SetIdentity(context.User.Email, true, null);
        await context.Link.Handler(context.User.Id, context.Request, "browser", CancellationToken.None);
        A.CallTo(() => context.Identities.Add(A<ExternalIdentity>.That.Matches(x => x.UserId == context.User.Id))).MustHaveHappenedOnceExactly();
    }
}
