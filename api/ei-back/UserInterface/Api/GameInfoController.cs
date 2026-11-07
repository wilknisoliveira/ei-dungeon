using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.GameInfo.Dtos;
using ei_back.Core.Application.UseCase.GameInfo.Interfaces;
using ei_back.Core.Application.UseCase.User.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace ei_back.UserInterface.Api
{
    [EnableRateLimiting("Authenticated")]
    [Route("api/game/[controller]")]
    [ApiController]
    public class GameInfoController : ControllerBase
    {
        private readonly ILogger<GameInfoController> _logger;
        private readonly IGetUserNameUseCase _getUserNameUseCase;
        private readonly ICreateGameInfoUseCase _createGameInfoUseCase;

        public GameInfoController(
            ILogger<GameInfoController> logger,
            IGetUserNameUseCase getUserNameUseCase,
            ICreateGameInfoUseCase createGameInfoUseCase)
        {
            _logger = logger;
            _getUserNameUseCase = getUserNameUseCase;
            _createGameInfoUseCase = createGameInfoUseCase;
        }

        [HttpPost]
        [ProducesResponseType(typeof(IEnumerable<GameInfoDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] IEnumerable<GameInfoDto> gameInfoDtos, CancellationToken cancellationToken = default)
        {
            var userName = _getUserNameUseCase.Handler(User);

            if (userName.IsNullOrEmpty())
            {
                var errorMessage = "Something went wrong while attempting to get the user logged credential.";
                _logger.LogError(errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }

            _logger.LogInformation($"Creating game info by user {userName}...");

            IEnumerable<GameInfoDto> result = await _createGameInfoUseCase.Handler(gameInfoDtos, cancellationToken);

            _logger.LogInformation($"Game info created.");

            return Ok(result);
        }
    }
}
