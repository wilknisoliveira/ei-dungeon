namespace ei_back.Infrastructure.Exceptions.ExceptionTypes
{
    public class BadGatewayException : Exception
    {
        public BadGatewayException(string? message) : base(message)
        {
        }
    }
}
