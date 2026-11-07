using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Email;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.Service.Verification;
using ei_back.Core.Application.Service.Verification.Interfaces;
using ei_back.Core.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.Service.Email;

public class VerificationEmailDeliveryTests
{
    [Fact]
    public async Task Delivery_UsesComposerAndForwardsMessageAndCancellationUnchanged()
    {
        var sender = A.Fake<IEmailSender>();
        var composer = A.Fake<IVerificationEmailComposer>();
        var delivery = new CodeDelivery("player@example.com", "000123", VerificationCodePurpose.PasswordRecovery,
            TimeSpan.FromMinutes(7));
        var email = new OutgoingEmail(delivery.Email, "Custom subject", "Custom text", "<p>Custom HTML</p>");
        A.CallTo(() => composer.Compose(delivery)).Returns(email);
        var flow = new VerificationFlowService(A.Fake<IUserRepository>(), A.Fake<IVerificationCodeRepository>(),
            A.Fake<IUnitOfWork>(), A.Fake<IVerificationCodeService>(), A.Fake<IVerificationCodeProtector>(),
            sender, composer, NullLogger<VerificationFlowService>.Instance);
        using var cancellation = new CancellationTokenSource();
        await flow.DeliverAsync(delivery, cancellation.Token);
        A.CallTo(() => composer.Compose(delivery)).MustHaveHappenedOnceExactly();
        A.CallTo(() => sender.SendAsync(email, cancellation.Token)).MustHaveHappenedOnceExactly();
        await flow.DeliverAsync(null, cancellation.Token);
        A.CallTo(() => sender.SendAsync(A<OutgoingEmail>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Preparation_PassesTheCreatedRecordsActualLifetimeToDelivery()
    {
        var codeService = new VerificationCodeService();
        var codes = A.Fake<IVerificationCodeService>();
        var repository = A.Fake<IVerificationCodeRepository>();
        var user = new UserEntity("player", "Player Name", "player@example.com", "hash", UserRole.CommonUser)
            { Id = Guid.NewGuid() };
        A.CallTo(() => codes.CanIssue(A<IEnumerable<ei_back.Core.Domain.Entity.VerificationCode>>._, A<DateTimeOffset>._)).Returns(true);
        A.CallTo(() => codes.Create(user.Id, user.Email, VerificationCodePurpose.EmailVerification, A<DateTimeOffset>._))
            .ReturnsLazily((Guid id, string email, VerificationCodePurpose purpose, DateTimeOffset now) =>
            {
                var record = codeService.Create(id, email, purpose, now);
                record.ExpiresAt = now.AddMinutes(7);
                return record;
            });
        var protector = A.Fake<IVerificationCodeProtector>();
        A.CallTo(() => protector.GenerateCode()).Returns("000123");
        var flow = new VerificationFlowService(A.Fake<IUserRepository>(), repository, A.Fake<IUnitOfWork>(),
            codes, protector, A.Fake<IEmailSender>(), ei_back.Tests.Infrastructure.LocalizationTestSupport.CreateComposer(), NullLogger<VerificationFlowService>.Instance);
        var delivery = await flow.PrepareAsync(user, VerificationCodePurpose.EmailVerification);
        delivery!.ValidFor.Should().Be(TimeSpan.FromMinutes(7));
        delivery.Code.Should().Be("000123");
    }
}
