namespace ei_back.Core.Domain.DomainExceptions;
public class RegistrationConflictException : Exception
{
    public RegistrationConflictException() : base("Invalid registration data.") { }
}