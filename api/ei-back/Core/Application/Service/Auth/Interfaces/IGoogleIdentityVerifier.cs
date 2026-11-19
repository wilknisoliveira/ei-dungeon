namespace ei_back.Core.Application.Service.Auth.Interfaces;

public interface IGoogleIdentityVerifier
{
    bool IsConfigured { get; }
    Task<GoogleIdentity> VerifyAsync(string credential, CancellationToken cancellationToken = default);
}
