using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Application.Utils;
using ei_back.Core.Application.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        private readonly IDeleteUserUseCase _deleteUserUseCase;
        private readonly IGetUserIdUseCase _getUserIdUseCase;

        public UserController(
            ILogger<UserController> logger,
            ICreateUserUseCase createUserUseCase,
            IUnitOfWork unitOfWork,
            IGetUserUseCase getUserUseCase,
            IDeleteUserUseCase deleteUserUseCase,
            IGetUserIdUseCase getUserIdUseCase)
        {
            _logger = logger;
            _createUserUseCase = createUserUseCase;
            _unitOfWork = unitOfWork;
            _getUserUseCase = getUserUseCase;
            _deleteUserUseCase = deleteUserUseCase;
            _getUserIdUseCase = getUserIdUseCase;
        }

        /// <summary>Creates a new user account</summary>
        /// <remarks>
        /// Rate limited: 3 requests per minute sliding window (Signup policy).
        /// Registration endpoint. No authentication required.
        ///
        /// Request body (UserDtoRequest):
        ///   - Password (string, 8-50 characters, required)
        ///   - Email (string, valid email format, required)
        ///
        /// Response 200 (UserDtoResponse):
        ///   - Email (string)
        ///
        /// Response 400: Email is already registered or validation fails.
        ///
        /// Password is BCrypt-hashed server-side. Duplicate email returns
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

            _logger.LogInformation("API: New user created - " + userDtoRequest.Email);

            return Ok(userDtoResponse);
        }

        /// <summary>Lists users with paginated search (Admin only)</summary>
        /// <remarks>
        /// Requires authentication with Admin role.
        ///
        /// Query parameters:
        ///   - sortDirection (string): "asc" or "desc"
        ///   - pageSize (int): results per page
        ///   - page (int): page number (1-based)
        ///   - name (string?, optional): filters results by email
        ///
        /// Response 200 (PagedSearchDto&lt;UserGetDtoResponse&gt;):
        ///   - CurrentPage (int)
        ///   - PageSize (int)
        ///   - TotalResults (int)
        ///   - SortDirection (string)
        ///   - Items (UserGetDtoResponse[]): users with Id and Email
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
            var requesterId = _getUserIdUseCase.Handler(User);

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
