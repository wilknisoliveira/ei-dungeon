namespace ei_back.Core.Domain.DomainExceptions;

public sealed class SupportTicketException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
