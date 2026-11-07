namespace ei_back.Infrastructure.Exceptions.ExceptionTypes;

public class ForbiddenException(string? message) : Exception(message);