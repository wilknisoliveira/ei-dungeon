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
        private readonly IGetUserIdUseCase _getUserIdUseCase;
        private readonly IGetPlaysUseCase _getPlaysUseCase;
        private readonly INewUserPlayUseCase _newUserPlayUseCase;


        public PlayController(
            ILogger<PlayController> logger,
            IUnitOfWork unitOfWork,
            IGetUserIdUseCase getUserIdUseCase,
            IGetPlaysUseCase getPlaysUseCase,
            INewUserPlayUseCase newUserPlayUseCase)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _getUserIdUseCase = getUserIdUseCase;
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
            var userId = _getUserIdUseCase.Handler(User);

            _logger.LogInformation($"Get the play list of the game {gameId} to user {userId}");

            var response = await _getPlaysUseCase.Handler(gameId, sortDirection, pageSize, page, userId, cancellationToken);

            return Ok(response);
        }

        /// <summary>Streams a first scene or game master turn.</summary>
        /// <remarks>Free requests supply an operation ID and opening flag. Committed confirms persistence and the debit.</remarks>
        [HttpPost]
        [ProducesResponseType(typeof(IAsyncEnumerable<StreamPlayDtoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin, CommonUser, PremiumUser")]
        public async IAsyncEnumerable<StreamPlayDtoResponse> CreateUserPlay([FromBody] PlayDtoRequest playDtoRequest, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var userId = _getUserIdUseCase.Handler(User);

            _logger.LogInformation($"New play by user {userId} to game {playDtoRequest.GameId}.");
            playDtoRequest.UsesSparks = !User.IsInRole("Admin") && !User.IsInRole("PremiumUser");

            await foreach (var chunk in _newUserPlayUseCase.Handler(playDtoRequest, userId, cancellationToken))
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
