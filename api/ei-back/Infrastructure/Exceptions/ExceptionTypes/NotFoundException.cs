namespace ei_back.Infrastructure.Exceptions.ExceptionTypes
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string? message) : base(message)
        {
        }
    }
}
