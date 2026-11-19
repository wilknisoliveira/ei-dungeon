using ei_back.Core.Application.Service.Auth;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Domain.DomainExceptions;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
namespace ei_back.Infrastructure.Authentication;

public class GoogleIdentityVerifier(IOptions<GoogleAuthOptions> options) : IGoogleIdentityVerifier
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ClientId);
    public async Task<GoogleIdentity> VerifyAsync(string credential, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) throw new GoogleAuthenticationException("google_unavailable");
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(credential,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { options.Value.ClientId } })
                .WaitAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload.Subject) || payload.Subject.Length > 255)
                throw new GoogleAuthenticationException("google_invalid_credential");
            return new GoogleIdentity(payload.Subject, payload.Email, payload.EmailVerified, payload.HostedDomain, payload.Nonce);
        }
        catch (InvalidJwtException) { throw new GoogleAuthenticationException("google_invalid_credential"); }
        catch (HttpRequestException) { throw new GoogleAuthenticationException("google_unavailable"); }
    }
}
