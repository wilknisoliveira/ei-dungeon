using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.GoogleAuth;

public class GoogleSigninUseCaseTests
{
    private readonly GoogleAuthUseCaseTestContext context = new();

    [Theory]
    [InlineData("person@gmail.com", true, null, true)]
    [InlineData("person@company.com", true, "company.com", true)]
    [InlineData("person@outlook.com", true, null, false)]
    [InlineData("person@gmail.com", false, null, false)]
    [InlineData(null, false, null, false)]
    public async Task Signin_NewIdentity_RegistersOnlyAuthoritativeVerifiedEmail(string? email, bool verified, string? domain, bool eligible)
    {
        context.SetIdentity(email, verified, domain);
        if (eligible)
        {
            await context.Signin.Handler(context.Request, "browser", CancellationToken.None);
            A.CallTo(() => context.Identities.Add(A<ExternalIdentity>.That.Matches(x => x.Subject == "subject"))).MustHaveHappenedOnceExactly();
            A.CallTo(() => context.Sessions.Create(A<UserEntity>.That.Matches(x => x.Password == null && x.EmailVerifiedAt != null && x.Role == UserRole.CommonUser)))
                .MustHaveHappenedOnceExactly();
        }
        else
        {
            Func<Task> action = () => context.Signin.Handler(context.Request, "browser", CancellationToken.None);
            (await action.Should().ThrowAsync<GoogleAuthenticationException>()).Which.Code.Should().Be("google_registration_required");
            A.CallTo(() => context.Identities.Add(A<ExternalIdentity>._)).MustNotHaveHappened();
        }
    }

    [Fact]
    public async Task Signin_LinkedSubject_UsesExistingAccountWithoutReplacingChangedEmail()
    {
        context.SetIdentity("changed@outlook.com", false, null);
        A.CallTo(() => context.Identities.FindBySubjectAsync("subject", A<CancellationToken>._)).Returns(new ExternalIdentity { UserId = context.User.Id });
        await context.Signin.Handler(context.Request, "browser", CancellationToken.None);
        A.CallTo(() => context.Sessions.Create(context.User)).MustHaveHappenedOnceExactly();
        context.User.Email.Should().Be("player@gmail.com");
    }

    [Fact]
    public async Task Signin_ExistingEmail_RequiresExplicitLinking()
    {
        A.CallTo(() => context.Users.FindByEmail(context.User.NormalizedEmail, A<CancellationToken>._)).Returns(context.User);
        Func<Task> action = () => context.Signin.Handler(context.Request, "browser", CancellationToken.None);
        (await action.Should().ThrowAsync<GoogleAuthenticationException>()).Which.Code.Should().Be("google_link_required");
        A.CallTo(() => context.Sessions.Create(A<UserEntity>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData("wrong-browser", "nonce", false)]
    [InlineData("browser", "wrong-nonce", false)]
    [InlineData("browser", "nonce", true)]
    public async Task Signin_InvalidBindingOrConsumedAttempt_DeniesSession(string browser, string nonce, bool consumed)
    {
        context.SetIdentity(context.User.Email, true, null, nonce);
        if (consumed) A.CallTo(() => context.Challenges.ConsumeAsync(context.Request.ChallengeId, A<DateTimeOffset>._, A<CancellationToken>._)).Returns(false);
        Func<Task> action = () => context.Signin.Handler(context.Request, browser, CancellationToken.None);
        await action.Should().ThrowAsync<GoogleAuthenticationException>();
        A.CallTo(() => context.Sessions.Create(A<UserEntity>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Signin_LinkPurposeCannotBeReusedForLogin()
    {
        context.ConfigureAttempt(GoogleAuthPurpose.Link, context.User.Id);
        Func<Task> action = () => context.Signin.Handler(context.Request, "browser", CancellationToken.None);
        await action.Should().ThrowAsync<GoogleAuthenticationException>();
    }
}
