using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.DomainExceptions;

namespace ei_back.Tests.Core.Application.UseCase.GoogleAuth;

public class GetCurrentAccountUseCaseTests
{
    private readonly GoogleAuthUseCaseTestContext context = new();

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task Handler_VerifiedAccount_ReturnsPersistedCapabilities(bool hasPassword, bool googleLinked)
    {
        context.User.Password = hasPassword ? "password-hash" : null;
        A.CallTo(() => context.Identities.FindByUserAsync(context.User.Id, A<CancellationToken>._))
            .Returns(googleLinked ? new ExternalIdentity { UserId = context.User.Id } : null);
        var account = await context.GetAccount.Handler(context.User.Id, CancellationToken.None);
        account.Id.Should().Be(context.User.Id);
        account.Email.Should().Be(context.User.Email);
        account.Role.Should().Be("CommonUser");
        account.HasPassword.Should().Be(hasPassword);
        account.GoogleLinked.Should().Be(googleLinked);
    }

    [Fact]
    public async Task Handler_UnverifiedAccount_RejectsAccess()
    {
        context.User.EmailVerifiedAt = null;
        Func<Task> action = () => context.GetAccount.Handler(context.User.Id, CancellationToken.None);
        await action.Should().ThrowAsync<GoogleAuthenticationException>();
    }
}
