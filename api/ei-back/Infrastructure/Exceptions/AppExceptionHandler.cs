using ei_back.Core.Domain.DomainExceptions;
using Microsoft.Extensions.Localization;
using ei_back.UserInterface.Api;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace ei_back.Infrastructure.Exceptions
{
    public class AppExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<AppExceptionHandler> _logger;
        private readonly IStringLocalizer<AuthController> _localizer;

        public AppExceptionHandler(ILogger<AppExceptionHandler> logger, IStringLocalizer<AuthController> localizer)
        {
            _logger = logger;
            _localizer = localizer;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            (int statusCode, string errorMessage) = exception switch
            {
                GoogleAuthenticationException google => (google.Code switch
                {
                    "google_unavailable" => StatusCodes.Status503ServiceUnavailable,
                    "google_registration_required" => StatusCodes.Status422UnprocessableEntity,
                    "google_link_required" or "google_email_mismatch" or "google_link_conflict" or "password_already_exists" => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status401Unauthorized
                }, _localizer["AuthGoogleFailure"].Value),
                ei_back.Core.Domain.DomainExceptions.EmailVerificationRequiredException => (StatusCodes.Status403Forbidden, "Email verification is required."),
                ei_back.Core.Domain.DomainExceptions.RegistrationConflictException => (StatusCodes.Status400BadRequest, "Invalid registration data."),
                SparkOperationException spark => (spark.Code switch
                {
                    "sparks_invalid_operation" => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status409Conflict
                }, spark.Message),
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
            if (exception is GoogleAuthenticationException googleError) exceptionResponse.Code = googleError.Code;
            if (exception is SparkOperationException sparkError) exceptionResponse.Code = sparkError.Code;
            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(exceptionResponse, cancellationToken);

            return true;
        }
    }
}
