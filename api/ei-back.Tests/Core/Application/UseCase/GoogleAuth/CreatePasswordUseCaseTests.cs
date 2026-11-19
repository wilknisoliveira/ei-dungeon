using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.GoogleAuth;

public class CreatePasswordUseCaseTests
{
    private readonly GoogleAuthUseCaseTestContext context = new();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreatePassword_RequiresLinkedSubjectAndCannotOverwriteExistingPassword(bool alreadyHasPassword)
    {
        context.ConfigureAttempt(GoogleAuthPurpose.CreatePassword, context.User.Id);
        context.User.Password = alreadyHasPassword ? "existing-hash" : null;
        A.CallTo(() => context.Identities.FindBySubjectAsync("subject", A<CancellationToken>._))
            .Returns(new ExternalIdentity { UserId = alreadyHasPassword ? context.User.Id : Guid.NewGuid() });
        var passwordRequest = new CreatePasswordDtoRequest { ChallengeId = context.Request.ChallengeId, Credential = context.Request.Credential, NewPassword = "new-password" };
        Func<Task> action = () => context.CreatePassword.Handler(context.User.Id, passwordRequest, "browser", CancellationToken.None);
        await action.Should().ThrowAsync<GoogleAuthenticationException>();
        context.User.Password.Should().Be(alreadyHasPassword ? "existing-hash" : null);
    }

    [Fact]
    public async Task CreatePassword_ValidFreshProof_EnablesPasswordCapability()
    {
        context.ConfigureAttempt(GoogleAuthPurpose.CreatePassword, context.User.Id);
        A.CallTo(() => context.Identities.FindBySubjectAsync("subject", A<CancellationToken>._)).Returns(new ExternalIdentity { UserId = context.User.Id });
        await context.CreatePassword.Handler(context.User.Id, new() { ChallengeId = context.Request.ChallengeId, Credential = context.Request.Credential, NewPassword = "new-password" }, "browser", CancellationToken.None);
        BCrypt.Net.BCrypt.Verify("new-password", context.User.Password).Should().BeTrue();
        (await context.GetAccount.Handler(context.User.Id, CancellationToken.None)).HasPassword.Should().BeTrue();
    }
}
