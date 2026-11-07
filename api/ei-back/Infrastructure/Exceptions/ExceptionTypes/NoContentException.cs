namespace ei_back.Infrastructure.Exceptions.ExceptionTypes
{
    public class NoContentException : Exception
    {
        public NoContentException(string? message) : base(message) { }
    }
}
