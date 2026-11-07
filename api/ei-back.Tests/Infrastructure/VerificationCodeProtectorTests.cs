using ei_back.Infrastructure.Verification;
using ei_back.Core.Application.Service.Verification;
using ei_back.Core.Domain.Enums;
using Microsoft.Extensions.Options;

namespace ei_back.Tests.Infrastructure;

public class VerificationCodeProtectorTests
{
    [Fact]
    public void Hash_BindsCodeToRecordPurposeOwnerAndEmail_AndPreservesLeadingZeroes()
    {
        var protector = new VerificationCodeProtector(Options.Create(new VerificationCodeOptions { HashKey = new string('k', 32) }));
        var code = new VerificationCodeService().Create(Guid.NewGuid(), "a@b.com", VerificationCodePurpose.EmailVerification, DateTimeOffset.UtcNow);
        code.CodeHash = protector.Hash(code, "000123");
        protector.Matches(code, "000123").Should().BeTrue();
        protector.Matches(code, "123").Should().BeFalse();
        protector.Matches(code, "000124").Should().BeFalse();
        var purpose = code.Purpose;
        code.Purpose = VerificationCodePurpose.PasswordRecovery;
        protector.Matches(code, "000123").Should().BeFalse();
        code.Purpose = purpose;
        code.UserId = Guid.NewGuid();
        protector.Matches(code, "000123").Should().BeFalse();
        Enumerable.Range(0, 100).Select(_ => protector.GenerateCode()).Should().OnlyContain(x => x.Length == 6 && x.All(c => c >= '0' && c <= '9'));
    }

    [Fact]
    public void MissingProtectionKey_FailsWithoutDefault()
    {
        Action act = () => new VerificationCodeProtector(Options.Create(new VerificationCodeOptions()));
        act.Should().Throw<InvalidOperationException>();
    }
}