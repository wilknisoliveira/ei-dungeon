namespace ei_back.Core.Domain.DomainExceptions;

public class GoogleAuthenticationException(string code) : Exception("Google authentication could not be completed.")
{
    public string Code { get; } = code;
}
