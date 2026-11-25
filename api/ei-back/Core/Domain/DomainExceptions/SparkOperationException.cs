namespace ei_back.Core.Domain.DomainExceptions;

public class SparkOperationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
