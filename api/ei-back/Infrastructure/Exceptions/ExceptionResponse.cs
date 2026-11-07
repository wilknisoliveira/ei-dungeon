namespace ei_back.Infrastructure.Exceptions
{
    public class ExceptionResponse
    {
        public ExceptionResponse(int statusCode, string typeName, string message)
        {
            StatusCode = statusCode;
            TypeName = typeName;
            Message = message;
        }

        public string? Code { get; set; }
        public int StatusCode { get; set; }
        public string TypeName { get; set; }
        public string Message { get; set; }
    }
}
