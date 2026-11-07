using ei_back.Core.Application.Service.Email;
using System.Text.RegularExpressions;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.Service.Verification;
using ei_back.Core.Application.Service.Verification.Interfaces;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Verification;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.User;

public class EmailVerificationUseCaseTests
{
    [Fact]
    public async Task Confirmation_PersistsFailuresThenVerifiesOnce_AndResendInvalidatesOldCode()
    {
        var users = A.Fake<IUserRepository>();
        var codes = A.Fake<IVerificationCodeRepository>();
        var sender = A.Fake<IEmailSender>();
        var transaction = new TestUnitOfWork();
        var user = new UserEntity("tester", "Tester", "test@example.com", "hash", UserRole.CommonUser) { Id = Guid.NewGuid() };
        var history = new List<VerificationCode>();
        A.CallTo(() => users.FindByEmail(A<string>._, A<CancellationToken>._)).Returns(user);
        A.CallTo(() => codes.GetHistoryAsync(user.Id, A<VerificationCodePurpose>._, A<DateTimeOffset>._, A<CancellationToken>._)).Returns(history);
        A.CallTo(() => codes.Add(A<VerificationCode>._)).Invokes((VerificationCode record) => history.Add(record));
        A.CallTo(() => codes.GetLatestAsync(user.Id, A<VerificationCodePurpose>._, A<CancellationToken>._))
            .ReturnsLazily(() => history.LastOrDefault());
        string plain = "";
        A.CallTo(() => sender.SendAsync(A<OutgoingEmail>._, A<CancellationToken>._))
            .Invokes((OutgoingEmail email, CancellationToken ct) =>
            { transaction.Committed.Should().BeTrue(); plain = Regex.Match(email.TextBody, @"\b[0-9]{6}\b").Value; });
        var flow = new VerificationFlowService(users, codes, transaction, new VerificationCodeService(),
            new VerificationCodeProtector(Options.Create(new VerificationCodeOptions { HashKey = new string('x', 32) })),
            sender, ei_back.Tests.Infrastructure.LocalizationTestSupport.CreateComposer(), NullLogger<VerificationFlowService>.Instance);
        var request = new RequestEmailVerificationUseCase(flow);
        await request.Handler(new EmailCodeRequestDto { Email = user.Email });
        history.Should().ContainSingle();
        await request.Handler(new EmailCodeRequestDto { Email = user.Email });
        history.Should().ContainSingle();
        var previous = history[0];
        previous.CreatedAt = previous.CreatedAt.AddMinutes(-2);
        await request.Handler(new EmailCodeRequestDto { Email = user.Email });
        previous.InvalidatedAt.Should().NotBeNull();
        var confirm = new ConfirmEmailVerificationUseCase(flow);
        (await confirm.Handler(new() { Email = user.Email, Code = "wrong" })).Should().Be(CodeConfirmationResult.Invalid);
        history.Last().FailedAttempts.Should().Be(1);
        transaction.Committed.Should().BeTrue();
        (await confirm.Handler(new() { Email = user.Email, Code = plain })).Should().Be(CodeConfirmationResult.Confirmed);
        user.EmailVerifiedAt.Should().NotBeNull();
        (await confirm.Handler(new() { Email = user.Email, Code = plain })).Should().Be(CodeConfirmationResult.Invalid);
    }

    [Fact]
    public async Task Request_UnknownUserOrDeliveryFailure_DoesNotExposeEligibility()
    {
        var users = A.Fake<IUserRepository>();
        var codes = A.Fake<IVerificationCodeRepository>();
        var sender = A.Fake<IEmailSender>();
        var transaction = new TestUnitOfWork();
        A.CallTo(() => users.FindByEmail(A<string>._, A<CancellationToken>._)).Returns((UserEntity?)null);
        var flow = new VerificationFlowService(users, codes, transaction, new VerificationCodeService(),
            A.Fake<IVerificationCodeProtector>(), sender, ei_back.Tests.Infrastructure.LocalizationTestSupport.CreateComposer(), NullLogger<VerificationFlowService>.Instance);
        await flow.RequestAsync("unknown@example.com", VerificationCodePurpose.EmailVerification);
        A.CallTo(() => sender.SendAsync(A<OutgoingEmail>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => sender.SendAsync(A<OutgoingEmail>._, A<CancellationToken>._))
            .ThrowsAsync(new InvalidOperationException("unavailable"));
        await flow.DeliverAsync(new CodeDelivery("a@b.com", "123456", VerificationCodePurpose.EmailVerification, TimeSpan.FromMinutes(10)));
    }
}
