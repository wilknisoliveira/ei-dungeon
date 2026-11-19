using ei_back.Core.Domain.Enums;
namespace ei_back.Core.Domain.Entity;

public class GoogleAuthChallenge : Base
{
    public Guid? UserId { get; set; }
    public GoogleAuthPurpose Purpose { get; set; }
    public string NonceHash { get; set; } = string.Empty;
    public string BrowserSecretHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}
