using ei_back.Core.Application.UseCase.Role.Dtos;
using ei_back.Core.Application.UseCase.Role.Interfaces;
using ei_back.Core.Application.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ei_back.UserInterface.Api
{
    [EnableRateLimiting("Authenticated")]
    [ApiController]
    [Route("api/user/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly ILogger<RoleController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGetAllRoleUseCase _getAllRoleUseCase;
        private readonly IApplyRolesUseCase _applyRolesUseCase;

        public RoleController(
            ILogger<RoleController> logger,
            IUnitOfWork unitOfWork,
            IGetAllRoleUseCase getAllRoleUseCase,
            IApplyRolesUseCase applyRolesUseCase)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _getAllRoleUseCase = getAllRoleUseCase;
            _applyRolesUseCase = applyRolesUseCase;
        }

        /// <summary>Lists all available roles (Admin only)</summary>
        /// <remarks>
        /// Requires authentication with Admin role.
        ///
        /// Response 200 (RoleDtoResponse[]):
        ///   - Name (string): role name
        ///   - Users (string[]): list of usernames assigned to this role
        ///
        /// No request body required.
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(RoleDtoResponse), StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("API: Getting all the roles");

            return Ok(await _getAllRoleUseCase.Handler());
        }

        /// <summary>Applies a role to a user (Admin only)</summary>
        /// <remarks>
        /// Requires authentication with Admin role.
        ///
        /// Request body (ApplyRoleDtoRequest):
        ///   - Id (Guid, required): target user's ID
        ///   - role (UserRole enum, required): Admin, CommonUser, or PremiumUser
        ///
        /// Response 200 (ApplyRoleDtoResponse):
        ///   - Id (Guid)
        ///   - UserName (string)
        ///   - role (UserRole)
        ///
        /// Response 400: Invalid or missing request body.
        /// </remarks>
        [HttpPut("apply")]
        [ProducesResponseType(typeof(ApplyRoleDtoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApplyRoles(
            [FromBody] ApplyRoleDtoRequest applyRoleDtoRequest,
            CancellationToken cancellationToken = default)
        {
            if (applyRoleDtoRequest == null) return BadRequest();
            var response = await _applyRolesUseCase.Handler(applyRoleDtoRequest);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("API: Roles applied to user");

            return Ok(response);
        }
    }
}
