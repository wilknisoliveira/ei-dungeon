using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Application.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace ei_back.UserInterface.Api
{
    [ApiController]
    [Route("api/user/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IRequestPasswordRecoveryUseCase _requestRecovery;
        private readonly IConfirmPasswordRecoveryUseCase _confirmRecovery;
        private readonly IRequestEmailVerificationUseCase _requestVerification;
        private readonly IConfirmEmailVerificationUseCase _confirmVerification;
        private readonly ISignInUseCase _signInUseCase;
        private readonly IRefreshTokenUseCase _refreshTokenUseCase;
        private readonly ILogoutUseCase _logoutUseCase;
        private readonly IChangePasswordUseCase _changePasswordUseCase;
        private readonly IGetUserNameUseCase _getUserNameUseCase;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<AuthController> _stringLocalizer;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IRequestPasswordRecoveryUseCase requestRecovery,
            IConfirmPasswordRecoveryUseCase confirmRecovery,
            IRequestEmailVerificationUseCase requestVerification,
            IConfirmEmailVerificationUseCase confirmVerification,
            ISignInUseCase signInUseCase,
            IRefreshTokenUseCase refreshTokenUseCase,
            ILogoutUseCase logoutUseCase,
            IChangePasswordUseCase changePasswordUseCase,
            IGetUserNameUseCase getUserNameUseCase,
            IUnitOfWork unitOfWork,
            IStringLocalizer<AuthController> stringLocalizer,
            ILogger<AuthController> logger)
        {
            _requestRecovery = requestRecovery;
            _confirmRecovery = confirmRecovery;
            _requestVerification = requestVerification;
            _confirmVerification = confirmVerification;
            _signInUseCase = signInUseCase;
            _refreshTokenUseCase = refreshTokenUseCase;
            _logoutUseCase = logoutUseCase;
            _changePasswordUseCase = changePasswordUseCase;
            _getUserNameUseCase = getUserNameUseCase;
            _unitOfWork = unitOfWork;
            _stringLocalizer = stringLocalizer;
            _logger = logger;
        }

        /// <summary>Requests or resends an email ownership code</summary>
        /// <remarks>Anonymous. Generic acknowledgement; delivery and account eligibility are not disclosed.</remarks>
        [AllowAnonymous]
        [EnableRateLimiting("VerificationRequest")]
        [HttpPost("email-verification")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> RequestEmailVerification(EmailCodeRequestDto request, CancellationToken cancellationToken)
        {
            await _requestVerification.Handler(request, cancellationToken);
            return Accepted();
        }

        /// <summary>Confirms email ownership with a six-digit code</summary>
        /// <remarks>Anonymous. Invalid, expired, exhausted and consumed codes return the same error. Does not sign in.</remarks>
        [AllowAnonymous]
        [EnableRateLimiting("VerificationConfirm")]
        [HttpPost("email-verification/confirm")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ConfirmEmailVerification(ConfirmEmailVerificationRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _confirmVerification.Handler(request, cancellationToken);
            return result == CodeConfirmationResult.Confirmed ? NoContent() : InvalidCode();
        }

        /// <summary>Requests a password recovery code for a verified email</summary>
        /// <remarks>Anonymous. Generic acknowledgement regardless of account eligibility, cooldown or delivery outcome.</remarks>
        [AllowAnonymous]
        [EnableRateLimiting("VerificationRequest")]
        [HttpPost("password-recovery")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> RequestPasswordRecovery(EmailCodeRequestDto request, CancellationToken cancellationToken)
        {
            await _requestRecovery.Handler(request, cancellationToken);
            return Accepted();
        }

        /// <summary>Sets a new password using email and a six-digit recovery code</summary>
        /// <remarks>Anonymous. Consumes the code and revokes refresh tokens atomically. Does not sign in. Existing access tokens expire normally.</remarks>
        [AllowAnonymous]
        [EnableRateLimiting("VerificationConfirm")]
        [HttpPost("password-recovery/confirm")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ConfirmPasswordRecovery(ConfirmPasswordRecoveryRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _confirmRecovery.Handler(request, cancellationToken);
            return result == CodeConfirmationResult.Confirmed ? NoContent() : InvalidCode();
        }
        private BadRequestObjectResult InvalidCode() => BadRequest(new ei_back.Infrastructure.Exceptions.ExceptionResponse(
            StatusCodes.Status400BadRequest, "InvalidVerificationCode", "Invalid code for email.") { Code = "invalid_verification_code" });
        /// <summary>Authenticates a user and returns a JWT token pair</summary>
        /// <remarks>
        /// Rate limited: 5 requests per minute sliding window (Login policy).
        ///
        /// Request body (LoginDtoRequest):
        ///   - UserName (string, 4-20 characters, required)
        ///   - Password (string, 4-50 characters, required)
        ///
        /// Response 200 (TokenDtoReponse):
        ///   - Authenticated (bool)
        ///   - Created (string, yyyy-MM-dd HH:mm:ss)
        ///   - Expiration (string, yyyy-MM-dd HH:mm:ss)
        ///   - AccessToken (string, JWT)
        ///   - RefreshToken (string)
        ///
        /// Response 403: Correct password but email is unverified (email_verification_required).
        /// Response 401: Invalid credentials.
        ///
        /// Supports both BCrypt and legacy SHA256 password hashing. Legacy
        /// passwords are automatically re-hashed to BCrypt on successful login.
        /// No authentication required (public endpoint).
        /// </remarks>
        [EnableRateLimiting("Login")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TokenDtoReponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("signin")]
        public IActionResult Signin([FromBody] LoginDtoRequest loginDtoRequest)
        {
            if (loginDtoRequest == null) return BadRequest(_stringLocalizer["AuthInvalidRequest"].Value);

            var token = _signInUseCase.Handler(loginDtoRequest);

            if (token == null) return Unauthorized();

            _unitOfWork.Commit();

            _logger.LogInformation("API: Logged user - " + loginDtoRequest.UserName);

            return Ok(token);
        }

        /// <summary>Refreshes an expired JWT access token using a valid refresh token</summary>
        /// <remarks>
        /// Rate limited: 10 requests per minute sliding window (Refresh policy).
        ///
        /// Request body (RefreshTokenDtoRequest):
        ///   - AccessToken (string, expired JWT, required)
        ///   - RefreshToken (string, required)
        ///
        /// Response 200 (TokenDtoReponse): New token pair (token rotation).
        /// The old refresh token is invalidated and replaced.
        ///
        /// Response 401: Invalid credentials if the refresh token is expired,
        /// malformed, or the user no longer exists.
        ///
        /// No authentication required (public endpoint).
        /// </remarks>
        [EnableRateLimiting("Refresh")]
        [HttpPost]
        [Route("refresh")]
        [ProducesResponseType(typeof(TokenDtoReponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Refresh([FromBody] RefreshTokenDtoRequest request)
        {
            var token = _refreshTokenUseCase.Handler(request);
            _unitOfWork.Commit();
            _logger.LogInformation("API: Token refreshed");
            return Ok(token);
        }

        /// <summary>Logs out a user by revoking their refresh token(s)</summary>
        /// <remarks>
        /// Requires authentication. Roles: Admin, CommonUser, PremiumUser.
        ///
        /// Request body (LogoutDtoRequest):
        ///   - RefreshToken (string?, optional)
        ///
        /// Behavior:
        ///   - If RefreshToken is provided: revokes that specific token only
        ///     (session logout).
        ///   - If RefreshToken is null or empty: revokes ALL refresh tokens
        ///     for the authenticated user (full logout).
        ///
        /// Response 200: Logout successful.
        /// </remarks>
        [EnableRateLimiting("Authenticated")]
        [HttpPost]
        [Route("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin, CommonUser, PremiumUser")]
        public IActionResult Logout([FromBody] LogoutDtoRequest request)
        {
            var userName = _getUserNameUseCase.Handler(User);
            _logoutUseCase.Handler(userName, request.RefreshToken);
            _unitOfWork.Commit();
            _logger.LogInformation("API: Logged out user - " + userName);
            return Ok();
        }

        /// <summary>Changes the password for the authenticated user</summary>
        /// <remarks>
        /// Requires authentication. Roles: Admin, CommonUser, PremiumUser.
        ///
        /// Request body (PasswordDtoRequest):
        ///   - CurrentPassword (string, 4-50 characters, required)
        ///   - NewPassword (string, 4-50 characters, required)
        ///
        /// Response 200 (UserGetDtoResponse):
        ///   - Id (Guid)
        ///   - UserName (string)
        ///   - FullName (string)
        ///   - Email (string)
        ///
        /// Response 400: Wrong current password or invalid request.
        ///
        /// Supports both BCrypt and legacy SHA256 verification of the current
        /// password. The new password is always BCrypt-hashed.
        /// </remarks>
        [EnableRateLimiting("Authenticated")]
        [HttpPatch]
        [ProducesResponseType(typeof(UserGetDtoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin, CommonUser, PremiumUser")]
        [Route("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] PasswordDtoRequest passwordDtoRequest)
        {
            if (passwordDtoRequest == null) return BadRequest(_stringLocalizer["AuthInvalidRequest"].Value);

            var userName = _getUserNameUseCase.Handler(User);
            var userChanged = await _changePasswordUseCase.Handler(userName, passwordDtoRequest);

            if (userChanged == null) return BadRequest();

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("API: Password changed for user - " + userName);

            return Ok(userChanged);
        }
    }
}
