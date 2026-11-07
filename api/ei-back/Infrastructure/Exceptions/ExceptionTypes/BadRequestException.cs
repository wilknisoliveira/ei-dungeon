namespace ei_back.Infrastructure.Exceptions.ExceptionTypes
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string? message) : base(message)
        {
        }
    }
}
