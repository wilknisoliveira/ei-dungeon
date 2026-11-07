using System.Runtime.CompilerServices;
using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Application.UseCase.Play.Interfaces;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Application.Utils;
using ei_back.Core.Application.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace ei_back.UserInterface.Api
{
    [EnableRateLimiting("Authenticated")]
    [Route("api/[controller]")]
    [ApiController]
    public class PlayController : ControllerBase
    {
        private readonly ILogger<PlayController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGetUserNameUseCase _getUserNameUseCase;
        private readonly IGetPlaysUseCase _getPlaysUseCase;
        private readonly INewUserPlayUseCase _newUserPlayUseCase;


        public PlayController(
            ILogger<PlayController> logger,
            IUnitOfWork unitOfWork,
            IGetUserNameUseCase getUserNameUseCase,
            IGetPlaysUseCase getPlaysUseCase,
            INewUserPlayUseCase newUserPlayUseCase)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _getUserNameUseCase = getUserNameUseCase;
            _getPlaysUseCase = getPlaysUseCase;
            _newUserPlayUseCase = newUserPlayUseCase;
        }

        /// <summary>Lists plays with paginated search</summary>
        /// <remarks>
        /// Requires authentication. Roles: Admin, CommonUser, PremiumUser.
        /// The game must belong to the authenticated user or a 404 is returned.
        ///
        /// Query parameters:
        ///   - gameId (guid, required): filters plays by game
        ///   - sortDirection (string): "asc" or "desc"
        ///   - pageSize (int): results per page
        ///   - page (int): page number (1-based)
        ///
        /// Response 200 (PagedSearchDto&lt;PlayDtoResponse&gt;):
        ///   - CurrentPage (int)
        ///   - PageSize (int)
        ///   - TotalResults (int)
        ///   - SortDirection (string)
        ///   - Items (PlayDtoResponse[]): Id, PlayType, Response, CreatedAt
        ///
        /// Response 400: Invalid sort direction, page size, or page number.
        /// Response 404: Game not found or not owned by the authenticated user.
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(PagedSearchDto<PlayDtoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin, CommonUser, PremiumUser")]
        public async Task<IActionResult> Get(
            [FromQuery] Guid gameId,
            [FromQuery] string sortDirection,
            [FromQuery] int pageSize,
            [FromQuery] int page,
            CancellationToken cancellationToken)
        {
            var userName = _getUserNameUseCase.Handler(User);

            if (userName.IsNullOrEmpty())
            {
                var errorMessage = "Something went wrong while attempting to get the user logged credential.";
                _logger.LogError(errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }

            _logger.LogInformation($"Get the play list of the game {gameId} to user {userName}");

            var response = await _getPlaysUseCase.Handler(gameId, sortDirection, pageSize, page, userName, cancellationToken);

            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(IAsyncEnumerable<StreamPlayDtoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin, PremiumUser")]
        public async IAsyncEnumerable<StreamPlayDtoResponse> CreateUserPlay([FromBody] PlayDtoRequest playDtoRequest, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var userName = _getUserNameUseCase.Handler(User);

            if (userName.IsNullOrEmpty())
            {
                var errorMessage = "Something went wrong while attempting to get the user logged credential.";
                _logger.LogError(errorMessage);

                yield return new StreamPlayDtoResponse { EventType = EventType.Error, Content = errorMessage };

                yield break;
            }

            _logger.LogInformation($"New play by user {userName} to game {playDtoRequest.GameId}.");

            await foreach (var chunk in _newUserPlayUseCase.Handler(playDtoRequest, userName, cancellationToken))
            {
                yield return chunk;
            }

            _logger.LogInformation($"User play process success.");

            yield return new StreamPlayDtoResponse
            {
                EventType = EventType.End,
            };
        }

    }
}
