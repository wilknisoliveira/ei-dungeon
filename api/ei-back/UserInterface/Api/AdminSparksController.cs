using ei_back.Core.Application.UseCase.Sparks.Dtos;
using ei_back.Core.Application.UseCase.Sparks.Interfaces;
using ei_back.Core.Application.UseCase.User.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ei_back.UserInterface.Api;

[ApiController]
[Route("api/admin/sparks")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("Authenticated")]
public sealed class AdminSparksController(
    IGetUserIdUseCase getUserId,
    IGetSparkAdministrationUseCase administration,
    IUpdateSparkSettingsUseCase update,
    IIncreaseSparkCapacityUseCase increase,
    IResetSparkCycleUseCase reset) : ControllerBase
{
    /// <summary>Reads settings, current metrics and the selected cycle's history.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SparkAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<SparkAdminResponse> Get([FromQuery] Guid? cycleId, CancellationToken cancellationToken) =>
        administration.Handler(cycleId, cancellationToken);

    /// <summary>Updates editable pilot defaults. Capacity changes apply to future cycles.</summary>
    [HttpPatch("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(SparkSettingsRequest settings, CancellationToken cancellationToken)
    {
        await update.Handler(settings, cancellationToken);
        return NoContent();
    }

    /// <summary>Increases the current cycle's capacity by the positive amount supplied.</summary>
    [HttpPost("cycles/{id:guid}/increases")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Increase(Guid id, SparkCycleIncreaseRequest request, CancellationToken cancellationToken)
    {
        await increase.Handler(id, getUserId.Handler(User), request.Amount, cancellationToken);
        return NoContent();
    }

    /// <summary>Opens the deficit-blocked cycle if credits cover its current control line and earlier commitments.</summary>
    [HttpPost("cycles/{id:guid}/reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reset(Guid id, CancellationToken cancellationToken)
    {
        return await reset.Handler(id, getUserId.Handler(User), cancellationToken)
            ? NoContent()
            : Conflict(new ProblemDetails { Title = "Sufficient OpenRouter coverage could not be verified." });
    }
}
