namespace ei_back.Infrastructure.Exceptions.ExceptionTypes
{
    public class InternalServerErrorException : Exception
    {
        public InternalServerErrorException(string? message) : base(message)
        {
        }
    }
}
