using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace ei_back.Infrastructure.Exceptions
{
    public class AppExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<AppExceptionHandler> _logger;

        public AppExceptionHandler(ILogger<AppExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            (int statusCode, string errorMessage) = exception switch
            {
                ei_back.Core.Domain.DomainExceptions.EmailVerificationRequiredException => (StatusCodes.Status403Forbidden, "Email verification is required."),
                ei_back.Core.Domain.DomainExceptions.RegistrationConflictException => (StatusCodes.Status400BadRequest, "Invalid registration data."),
                BadGatewayException badGatewayException => (StatusCodes.Status502BadGateway, badGatewayException.Message),
                BadRequestException badRequestException => (StatusCodes.Status400BadRequest, badRequestException.Message),
                NotFoundException notFoundException => (StatusCodes.Status404NotFound, notFoundException.Message),
                NoContentException noContentException => (StatusCodes.Status204NoContent, noContentException.Message),
                InternalServerErrorException internalServerErrorException => (StatusCodes.Status500InternalServerError, internalServerErrorException.Message),
                ForbiddenException forbiddenException => (StatusCodes.Status403Forbidden, forbiddenException.Message),
                UnauthorizedException unauthorizedException => (StatusCodes.Status401Unauthorized, unauthorizedException.Message),
                _ => (StatusCodes.Status500InternalServerError, "Something went wrong")
            };

            _logger.LogError(exception, $"{statusCode} - {exception.GetType().Name}: {errorMessage}. StackTrace: {exception.StackTrace}");

            ExceptionResponse exceptionResponse = new(statusCode, exception.GetType().Name, errorMessage);
            
            if (exception is ei_back.Core.Domain.DomainExceptions.EmailVerificationRequiredException)
                exceptionResponse.Code = "email_verification_required";
            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(exceptionResponse, cancellationToken);

            return true;
        }
    }
}
