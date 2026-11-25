using System.Globalization;
using ei_back.Core.Application.UseCase.Sparks.Dtos;
using ei_back.Core.Application.UseCase.Sparks.Interfaces;
using ei_back.Core.Application.UseCase.User.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ei_back.UserInterface.Api;

[ApiController]
[Route("api/sparks")]
[EnableRateLimiting("Authenticated")]
public sealed class SparksController(
    IGetUserIdUseCase getUserId,
    IGetSparkAvailabilityUseCase availability,
    IGetSparkStatusUseCase status,
    IRequestSparksUseCase request,
    IDecideSparkOfferUseCase decide) : ControllerBase
{
    /// <summary>Returns the maximum initial Sparks grant.</summary>
    [HttpGet("availability")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true)]
    [ProducesResponseType(typeof(SparkAvailabilityResponse), StatusCodes.Status200OK)]
    public Task<SparkAvailabilityResponse> Availability(CancellationToken cancellationToken) =>
        availability.Handler(cancellationToken);

    /// <summary>Reads the Free player's current Sparks balance, expiry and request eligibility.</summary>
    [HttpGet]
    [Authorize(Roles = "CommonUser")]
    [ProducesResponseType(typeof(SparkStatusResponse), StatusCodes.Status200OK)]
    public Task<SparkStatusResponse> Get(CancellationToken cancellationToken) =>
        status.Handler(getUserId.Handler(User), cancellationToken);

    /// <summary>Requests an immediate grant or returns an explicit queue offer.</summary>
    [HttpPost]
    [Authorize(Roles = "CommonUser")]
    [ProducesResponseType(typeof(SparkStatusResponse), StatusCodes.Status200OK)]
    public Task<SparkStatusResponse> RequestSparks(CancellationToken cancellationToken) =>
        request.Handler(getUserId.Handler(User), CultureInfo.CurrentUICulture.Name, cancellationToken);

    /// <summary>Accepts or explicitly declines the signed-in player's queue offer.</summary>
    [HttpPatch("queue/{id:guid}")]
    [Authorize(Roles = "CommonUser")]
    [ProducesResponseType(typeof(SparkStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<SparkStatusResponse> Decide(Guid id, SparkDecisionRequest decision, CancellationToken cancellationToken) =>
        decide.Handler(getUserId.Handler(User), id, decision.Accepted!.Value, cancellationToken);
}
