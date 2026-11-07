using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Domain.Entity;

public class VerificationCode : Base
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public VerificationCodePurpose Purpose { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset? InvalidatedAt { get; set; }

    public bool IsUsable(DateTimeOffset now) =>
        ConsumedAt == null && InvalidatedAt == null && ExpiresAt > now && FailedAttempts < 5;

    public void RecordFailure(DateTimeOffset now)
    {
        if (!IsUsable(now)) return;
        FailedAttempts++;
        UpdatedAt = now;
        if (FailedAttempts >= 5) InvalidatedAt = now;
    }
}