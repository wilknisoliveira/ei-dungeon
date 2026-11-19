using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Enums;
using UserEntity = ei_back.Core.Domain.Entity.User;
namespace ei_back.Tests.Core.Application.UseCase.User;

public class SigninUseCaseTests
{
    private readonly IUserRepository users = A.Fake<IUserRepository>();
    private readonly IEncryptionService encryption = A.Fake<IEncryptionService>();
    private readonly IApplicationSessionService sessions = A.Fake<IApplicationSessionService>();
    private readonly LoginDtoRequest request = new() { Email = "player@example.com", Password = "password123" };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Handler_AbsentLocalPassword_DoesNotIssueSession(string? password)
    {
        A.CallTo(() => users.FindByEmail(request.Email, CancellationToken.None)).Returns(new UserEntity(request.Email, password, UserRole.CommonUser));
        (await new SigninUseCase(users, encryption, sessions).Handler(request)).Should().BeNull();
        A.CallTo(() => sessions.Create(A<UserEntity>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handler_UnverifiedUser_ChecksPasswordBeforeVerificationError(bool valid)
    {
        var user = new UserEntity(request.Email, "$2a$hash", UserRole.CommonUser);
        A.CallTo(() => users.FindByEmail(request.Email, CancellationToken.None)).Returns(user);
        A.CallTo(() => encryption.VerifyBcryptHash(request.Password, user.Password!)).Returns(valid);
        var useCase = new SigninUseCase(users, encryption, sessions);
        if (valid)
        {
            Func<Task> action = () => useCase.Handler(request);
            await action.Should().ThrowAsync<EmailVerificationRequiredException>();
        }
        else (await useCase.Handler(request)).Should().BeNull();
        A.CallTo(() => sessions.Create(A<UserEntity>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handler_VerifiedCredentials_IssuesSessionAndUpgradesLegacyHash(bool bcrypt)
    {
        var user = new UserEntity(request.Email, bcrypt ? "$2a$hash" : "legacy", UserRole.CommonUser) { EmailVerifiedAt = DateTimeOffset.UtcNow };
        A.CallTo(() => users.FindByEmail(request.Email, CancellationToken.None)).Returns(user);
        A.CallTo(() => encryption.VerifyBcryptHash(request.Password, "$2a$hash")).Returns(true);
        A.CallTo(() => encryption.ComputeSha256(request.Password)).Returns("legacy");
        A.CallTo(() => encryption.ComputeBcryptHash(request.Password)).Returns("$2a$upgraded");
        var token = new TokenDtoReponse(true, "created", "expires", "access", "refresh");
        A.CallTo(() => sessions.Create(user)).Returns(token);
        (await new SigninUseCase(users, encryption, sessions).Handler(request)).Should().BeSameAs(token);
        user.Password.Should().Be(bcrypt ? "$2a$hash" : "$2a$upgraded");
    }

    [Fact]
    public async Task Handler_UnknownEmail_ReturnsNoSession()
    {
        A.CallTo(() => users.FindByEmail(request.Email, CancellationToken.None)).Returns((UserEntity?)null);
        (await new SigninUseCase(users, encryption, sessions).Handler(request)).Should().BeNull();
    }
}
