using ei_back.Core.Application.Service.Verification.Interfaces;
using ei_back.Core.Application.Utils;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.Service.Verification;

public class VerificationCodeService : IVerificationCodeService
{
    // History must already be scoped to one user and purpose by the repository.
    public bool CanIssue(IEnumerable<VerificationCode> history, DateTimeOffset now)
    {
        var recent = history.Where(x => x.CreatedAt > now.AddHours(-1)).ToList();
        return recent.Count < 5 && !recent.Any(x => x.CreatedAt > now.AddMinutes(-1));
    }

    public VerificationCode Create(Guid userId, string email, VerificationCodePurpose purpose, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, Email = EmailNormalizer.Normalize(email), Purpose = purpose,
        CreatedAt = now, UpdatedAt = now, ExpiresAt = now.AddMinutes(10)
    };

    public bool Confirm(VerificationCode code, Guid userId, string email, VerificationCodePurpose purpose,
        DateTimeOffset now, bool matches)
    {
        if (code.UserId != userId || code.Email != EmailNormalizer.Normalize(email) || code.Purpose != purpose || !code.IsUsable(now))
            return false;
        if (!matches)
        {
            code.RecordFailure(now);
            return false;
        }
        code.ConsumedAt = now;
        code.UpdatedAt = now;
        return true;
    }
}