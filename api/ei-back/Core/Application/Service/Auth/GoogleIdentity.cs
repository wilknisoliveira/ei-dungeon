namespace ei_back.Core.Application.Service.Auth;

public record GoogleIdentity(string Subject, string? Email, bool EmailVerified, string? HostedDomain, string? Nonce)
{
    public bool HasAuthoritativeEmail => EmailVerified && !string.IsNullOrWhiteSpace(Email) &&
        (Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(HostedDomain));
}
