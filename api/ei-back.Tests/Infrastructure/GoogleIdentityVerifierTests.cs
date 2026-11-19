using ei_back.Core.Domain.DomainExceptions;
using ei_back.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
namespace ei_back.Tests.Infrastructure;

public class GoogleIdentityVerifierTests
{
    [Fact]
    public async Task Verify_MissingClientId_ReturnsUnavailableWithoutProviderCall()
    {
        var verifier = new GoogleIdentityVerifier(Options.Create(new GoogleAuthOptions()));
        verifier.IsConfigured.Should().BeFalse();
        Func<Task> action = () => verifier.VerifyAsync("credential");
        (await action.Should().ThrowAsync<GoogleAuthenticationException>()).Which.Code.Should().Be("google_unavailable");
    }

    [Fact]
    public async Task Verify_MalformedCredential_RejectsWithoutExposingToken()
    {
        var verifier = new GoogleIdentityVerifier(Options.Create(new GoogleAuthOptions { ClientId = "test.apps.googleusercontent.com" }));
        Func<Task> action = () => verifier.VerifyAsync("invalid-provider-secret");
        var error = (await action.Should().ThrowAsync<GoogleAuthenticationException>()).Which;
        error.Code.Should().Be("google_invalid_credential");
        error.ToString().Should().NotContain("invalid-provider-secret");
    }
}
