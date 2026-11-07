using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.Service.Verification.Interfaces;

public interface IVerificationCodeService
{
    bool CanIssue(IEnumerable<VerificationCode> history, DateTimeOffset now);
    VerificationCode Create(Guid userId, string email, VerificationCodePurpose purpose, DateTimeOffset now);
    bool Confirm(VerificationCode code, Guid userId, string email, VerificationCodePurpose purpose,
        DateTimeOffset now, bool matches);
}