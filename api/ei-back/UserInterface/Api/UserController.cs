using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Application.Utils;
using ei_back.Core.Application.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ei_back.UserInterface.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICreateUserUseCase _createUserUseCase;
        private readonly IGetUserUseCase _getUserUseCase;
        private readonly ICheckUserInfoUseCase _checkUserInfoUseCase;
        private readonly IDeleteUserUseCase _deleteUserUseCase;
        private readonly IGetUserNameUseCase _getUserNameUseCase;

        public UserController(
            ILogger<UserController> logger,
            ICreateUserUseCase createUserUseCase,
            IUnitOfWork unitOfWork,
            IGetUserUseCase getUserUseCase,
            ICheckUserInfoUseCase checkUserInfoUseCase,
            IDeleteUserUseCase deleteUserUseCase,
            IGetUserNameUseCase getUserNameUseCase)
        {
            _logger = logger;
            _createUserUseCase = createUserUseCase;
            _unitOfWork = unitOfWork;
            _getUserUseCase = getUserUseCase;
            _checkUserInfoUseCase = checkUserInfoUseCase;
            _deleteUserUseCase = deleteUserUseCase;
            _getUserNameUseCase = getUserNameUseCase;
        }

        /// <summary>Creates a new user account</summary>
        /// <remarks>
        /// Rate limited: 3 requests per minute sliding window (Signup policy).
        /// Registration endpoint. No authentication required.
        ///
        /// Request body (UserDtoRequest):
        ///   - UserName (string, 4-20 characters, required)
        ///   - FullName (string, 4-50 characters, required)
        ///   - Password (string, 4-50 characters, required)
        ///   - Email (string, valid email format, required)
        ///
        /// Response 200 (UserDtoResponse):
        ///   - UserName (string)
        ///   - FullName (string)
        ///   - Email (string)
        ///
        /// Response 400: Username already exists or validation fails.
        ///
        /// Password is BCrypt-hashed server-side. Duplicate username returns
        /// a generic "Invalid registration data." message to prevent user
        /// enumeration. New users default to CommonUser role.
        /// </remarks>
        [EnableRateLimiting("Signup")]
        [HttpPost]
        [ProducesResponseType(typeof(UserDtoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] UserDtoRequest userDtoRequest, CancellationToken cancellationToken = default)
        {
            if (userDtoRequest == null) return BadRequest();
            var userDtoResponse = await _createUserUseCase.Handler(userDtoRequest);

            _logger.LogInformation("API: New user created - " + userDtoRequest.UserName);

            return Ok(userDtoResponse);
        }

        /// <summary>Checks availability of username and/or email</summary>
        /// <remarks>
        /// Rate limited: 20 requests per minute sliding window (UsernameCheck policy).
        /// No authentication required. Public endpoint.
        ///
        /// Query parameters (both optional):
        ///   - username (string): checks if this username is taken
        ///   - email (string): checks if this email is taken
        ///
        /// Response 200 (UserInfoCheckResponseDto):
        ///   - UsernameAvailable (bool)
        ///   - EmailAvailable (bool)
        ///
        /// Only checks parameters that are provided. Omitted parameters
        /// default to available (true).
        /// </remarks>
        [EnableRateLimiting("UsernameCheck")]
        [HttpGet("check-userinfo")]
        [ProducesResponseType(typeof(UserInfoCheckResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckUserInfo([FromQuery] string? username, [FromQuery] string? email)
        {
            var result = await _checkUserInfoUseCase.Handler(username, email);
            return Ok(result);
        }

        /// <summary>Lists users with paginated search (Admin only)</summary>
        /// <remarks>
        /// Requires authentication with Admin role.
        ///
        /// Query parameters:
        ///   - sortDirection (string): "asc" or "desc"
        ///   - pageSize (int): results per page
        ///   - page (int): page number (1-based)
        ///   - name (string?, optional): filters results by username
        ///
        /// Response 200 (PagedSearchDto&lt;UserGetDtoResponse&gt;):
        ///   - CurrentPage (int)
        ///   - PageSize (int)
        ///   - TotalResults (int)
        ///   - SortDirection (string)
        ///   - Items (UserGetDtoResponse[]): array of users with Id, UserName,
        ///     FullName, Email
        ///
        /// Response 400: Invalid sort direction, page size, or page number.
        /// Sort direction defaults to "desc" if not "asc".
        /// Page size defaults to 10 if less than 1.
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(PagedSearchDto<UserGetDtoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Get(
            [FromQuery] string? name,
            [FromQuery] string sortDirection,
            [FromQuery] int pageSize,
            [FromQuery] int page,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("API: Getting paged user list");

            return Ok(await _getUserUseCase.Handler(name, sortDirection, pageSize, page, cancellationToken));
        }

        /// <summary>Deletes a user account and all associated data</summary>
        /// <remarks>
        /// Rate limited: 120 requests per minute sliding window (Authenticated policy).
        /// Requires authentication. Roles: Admin, CommonUser, PremiumUser.
        ///
        /// Authorization logic:
        ///   - Admin users can delete any user by ID.
        ///   - Non-admin users can only delete their own account. If the
        ///     route {id} does not match the authenticated user's ID, a
        ///     403 Forbidden is returned.
        ///
        /// Route parameter:
        ///   - id (Guid): the ID of the user to delete
        ///
        /// Behavior:
        ///   - Cascades deletion to all associated data (games, players,
        ///     plays, refresh tokens) via database cascade constraints.
        ///
        /// Response 200: Account deleted successfully.
        /// Response 401: Not authenticated.
        /// Response 403: Authenticated but not authorized (non-admin
        ///   attempting to delete another user).
        /// Response 404: No user found with the given ID.
        /// </remarks>
        [EnableRateLimiting("Authenticated")]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin, CommonUser, PremiumUser")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var requesterIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(requesterIdClaim) || !Guid.TryParse(requesterIdClaim, out var requesterId))
                return Forbid();

            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && requesterId != id)
                return Forbid();

            await _deleteUserUseCase.Handler(id, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("API: User deleted - {UserId}", id);

            return Ok();
        }
    }
}
