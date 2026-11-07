using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.Game.Interfaces;
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
    public class GameController : ControllerBase
    {
        private readonly ILogger<GameController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICreateGameUseCase _createGameUseCase;
        private readonly IGetUserNameUseCase _getUserNameUseCase;
        private readonly IGetGamesUseCase _getGamesUseCase;
        private readonly IGetGameByIdAndUserUseCase _getGameByIdAndUserUseCase;
        private readonly IDeleteGameUseCase _deleteGameUseCase;
        private readonly IUpdateGameUseCase _updateGameUseCase;

        public GameController(
            ILogger<GameController> logger,
            IUnitOfWork unitOfWork,
            ICreateGameUseCase createGameUseCase,
            IGetUserNameUseCase getUserNameUseCase,
            IGetGamesUseCase getGamesUseCase, 
            IGetGameByIdAndUserUseCase getGameByIdAndUserUseCase, 
            IDeleteGameUseCase deleteGameUseCase,
            IUpdateGameUseCase updateGameUseCase)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _createGameUseCase = createGameUseCase;
            _getUserNameUseCase = getUserNameUseCase;
            _getGamesUseCase = getGamesUseCase;
            _getGameByIdAndUserUseCase = getGameByIdAndUserUseCase;
            _deleteGameUseCase = deleteGameUseCase;
            _updateGameUseCase = updateGameUseCase;
        }

        [HttpPost]
        [ProducesResponseType(typeof(GameDtoResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin, PremiumUser")]
        public async Task<IActionResult> Create([FromBody] GameDtoRequest gameDtoRequest, CancellationToken cancellationToken = default)
        {
            var userName = _getUserNameUseCase.Handler(User);

            if (userName.IsNullOrEmpty())
            {
                var errorMessage = "Something went wrong while attempting to get the user logged credential.";
                _logger.LogError(errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }

            _logger.LogInformation($"Creating {gameDtoRequest.Name} game to user {userName}...");

            var gameDtoResponse = await _createGameUseCase.Handler(gameDtoRequest, userName, cancellationToken);
            var changedItems = await _unitOfWork.CommitAsync(cancellationToken);

            if (changedItems == 0)
            {
                var errorMessage = "Something went wrong while attempting to create game.";
                _logger.LogError(errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }

            _logger.LogInformation($"Game {gameDtoRequest.Name} created.");

            return Ok(gameDtoResponse);
        }

        /// <summary>Lists games with paginated search</summary>
        /// <remarks>
        /// Requires authentication. Roles: Admin, CommonUser, PremiumUser.
        /// Results are scoped to the authenticated user's games.
        ///
        /// Query parameters:
        ///   - sortDirection (string): "asc" or "desc"
        ///   - pageSize (int): results per page
        ///   - page (int): page number (1-based)
        ///
        /// Response 200 (PagedSearchDto&lt;GameDtoResponse&gt;):
        ///   - CurrentPage (int)
        ///   - PageSize (int)
        ///   - TotalResults (int)
        ///   - SortDirection (string)
        ///   - Items (GameDtoResponse[]): Id, Name, OwnerUserId, GameStatus
        ///
        /// Response 400: Invalid sort direction, page size, or page number.
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(PagedSearchDto<GameDtoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin, CommonUser, PremiumUser")]
        public async Task<IActionResult> Get(
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

            _logger.LogInformation($"Get the list game to user {userName}");

            var response = await _getGamesUseCase.Handler(sortDirection, pageSize, page, userName, cancellationToken);

            return Ok(response);
        }

        [HttpGet("{gameId}")]
        [ProducesResponseType(typeof(GameDtoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin, CommonUser, PremiumUser")]
        public async Task<IActionResult> GetById(Guid gameId, CancellationToken cancellationToken)
        {
            var userName = _getUserNameUseCase.Handler(User);
            
            _logger.LogDebug("Get the info from game {gameId}", gameId);
            
            GameDtoResponse response = await _getGameByIdAndUserUseCase.Handler(gameId, userName, cancellationToken);
            return Ok(response);
        }

        [HttpDelete("{gameId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin, CommonUser, PremiumUser")]
        public async Task<IActionResult> Delete(Guid gameId, CancellationToken cancellationToken)
        {
            var userName = _getUserNameUseCase.Handler(User);
            
            _logger.LogDebug("Delete the game '{gameId}'", gameId);
            
            await _deleteGameUseCase.Handler(gameId, userName, cancellationToken);
            
            var changedItems = await _unitOfWork.CommitAsync(cancellationToken);
            if (changedItems == 0)
            {
                _logger.LogError("Something went wrong while attempting to delete the game.");
                return StatusCode(
                    StatusCodes.Status500InternalServerError, 
                    "Something went wrong while attempting to delete the game.");
            }
            
            _logger.LogDebug("Game '{gameId}' deleted successfully.'", gameId);
            return NoContent();
        }

        /// <summary>Updates a game's name and/or language</summary>
        /// <remarks>
        /// Requires authentication. Roles: Admin, PremiumUser.
        /// Only non-null fields in the request body are updated.
        ///
        /// Request body (all fields optional):
        ///   - name (string): new game name (2-20 chars)
        ///   - gameLanguage (enum): new language — 'Portuguese', 'English', or 'Spanish'
        ///
        /// Response 200 (GameDtoResponse):
        ///   - Id, Name, OwnerUserId, GameLanguage, GameStatus, LastPlayedAt
        ///
        /// Response 404: Game not found or not owned by the user.
        /// </remarks>
        [HttpPatch("{gameId}")]
        [ProducesResponseType(typeof(GameDtoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin, CommonUser, PremiumUser")]
        public async Task<IActionResult> Update(Guid gameId, [FromBody] UpdateGameDtoRequest request, CancellationToken cancellationToken)
        {
            var userName = _getUserNameUseCase.Handler(User);

            if (userName.IsNullOrEmpty())
            {
                var errorMessage = "Something went wrong while attempting to get the user logged credential.";
                _logger.LogError(errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }

            _logger.LogInformation("Updating game {gameId} for user {userName}...", gameId, userName);

            var gameDtoResponse = await _updateGameUseCase.Handler(gameId, request, userName, cancellationToken);
            var changedItems = await _unitOfWork.CommitAsync(cancellationToken);

            if (changedItems == 0)
            {
                var errorMessage = "Something went wrong while attempting to update the game.";
                _logger.LogError(errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }

            _logger.LogInformation("Game {gameId} updated.", gameId);

            return Ok(gameDtoResponse);
        }
        
    }
}
