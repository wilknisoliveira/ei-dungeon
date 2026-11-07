namespace ei_back.Core.Domain.DomainExceptions;
public class EmailVerificationRequiredException : Exception
{
    public EmailVerificationRequiredException() : base("Email verification is required.") { }
}