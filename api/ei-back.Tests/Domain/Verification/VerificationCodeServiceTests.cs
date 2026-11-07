using ei_back.Core.Application.Service.Verification;
using ei_back.Core.Domain.Enums;

namespace ei_back.Tests.Domain.Verification;

public class VerificationCodeServiceTests
{
    private readonly VerificationCodeService _service = new();

    [Fact]
    public void Confirmation_RequiresMatchingOwnerEmailPurpose_AndSingleUse()
    {
        var now = DateTimeOffset.UtcNow;
        var code = _service.Create(Guid.NewGuid(), "owner@example.com", VerificationCodePurpose.EmailVerification, now);
        _service.Confirm(code, Guid.NewGuid(), code.Email, code.Purpose, now, true).Should().BeFalse();
        _service.Confirm(code, code.UserId, "other@example.com", code.Purpose, now, true).Should().BeFalse();
        _service.Confirm(code, code.UserId, code.Email, VerificationCodePurpose.PasswordRecovery, now, true).Should().BeFalse();
        _service.Confirm(code, code.UserId, " OWNER@example.com ", code.Purpose, now, true).Should().BeTrue();
        _service.Confirm(code, code.UserId, code.Email, code.Purpose, now, true).Should().BeFalse();
    }

    [Fact]
    public void FiveFailures_InvalidateCodeWithoutConsumingIt()
    {
        var now = DateTimeOffset.UtcNow;
        var code = _service.Create(Guid.NewGuid(), "owner@example.com", VerificationCodePurpose.PasswordRecovery, now);
        for (var i = 0; i < 5; i++)
            _service.Confirm(code, code.UserId, code.Email, code.Purpose, now, false).Should().BeFalse();
        code.FailedAttempts.Should().Be(5);
        code.InvalidatedAt.Should().Be(now);
        code.ConsumedAt.Should().BeNull();
        _service.Confirm(code, code.UserId, code.Email, code.Purpose, now, true).Should().BeFalse();
    }

    [Fact]
    public void Expiration_IsExclusive_AndInvalidationPreventsConfirmation()
    {
        var now = DateTimeOffset.UtcNow;
        var code = _service.Create(Guid.NewGuid(), "a@b.com", VerificationCodePurpose.EmailVerification, now);
        _service.Confirm(code, code.UserId, code.Email, code.Purpose, now.AddMinutes(10), true).Should().BeFalse();
        code.InvalidatedAt = now;
        _service.Confirm(code, code.UserId, code.Email, code.Purpose, now, true).Should().BeFalse();
    }

    [Fact]
    public void Issuance_UsesRollingMinuteAndHourIncludingConsumedHistory()
    {
        var now = DateTimeOffset.UtcNow;
        var history = Enumerable.Range(1, 5).Select(i =>
            _service.Create(Guid.NewGuid(), "a@b.com", VerificationCodePurpose.EmailVerification, now.AddMinutes(-i * 2))).ToList();
        _service.CanIssue(history, now).Should().BeFalse();
        _service.CanIssue(history, now.AddHours(1)).Should().BeTrue();
        history = new() { _service.Create(Guid.NewGuid(), "a@b.com", VerificationCodePurpose.EmailVerification, now) };
        history[0].ConsumedAt = now;
        _service.CanIssue(history, now.AddSeconds(59)).Should().BeFalse();
        _service.CanIssue(history, now.AddMinutes(1)).Should().BeTrue();
    }
}